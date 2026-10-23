# Agent bridge (AFK+)

The agent bridge is an out-of-band channel between this client and the AFK+ host (`afk-host` on the cloud
machine, which relays to `afk-agent`). It reports what a trade needs to see (chat lines with their click
actions, windows and their items, the player inventory) and takes the few window actions a trade needs (a
click, an anvil name, closing a window). It is the third gate of AFK+'s safety model: it only takes
structured window operations, never a text command.

Code: `MinecraftClient/ChatBots/AgentBridge.cs` (events and operations),
`AgentBridgeChannel.cs` (descriptors, framing, queues), `AgentOperation.cs` (operation parsing),
`AgentBridgeItems.cs` (items), `Protocol/Message/ChatClick.cs` (click actions of chat components).
Tests: `MinecraftClient.Tests/AgentBridgeTests.cs`.

Contract version: **1** (the `v` of `hello`).

## Activation

The bridge runs only when the host names both descriptors in the environment:

| Variable | Value | Meaning |
| --- | --- | --- |
| `MCC_AGENT_FD_OUT` | descriptor number, ≥ 3 (normally `3`) | the client writes events here |
| `MCC_AGENT_FD_IN` | descriptor number, ≥ 3 (normally `4`) | the client reads operations here |

- On Linux these are inherited file descriptors (pipes or sockets). `afk-host` opens them only for an AFK+
  bot; plain AFK bots and the desktop launcher never set the variables, so nothing changes for them.
- On Windows (development only) the numbers are C runtime descriptors passed the way the C runtime, libuv
  and Node.js pass extra stdio entries (`STARTUPINFO.lpReserved2`). Without such a table the number is taken
  as a raw inheritable handle value. `child_process.spawn(exe, args, { stdio: ['pipe', 'pipe', 'pipe',
  'pipe', 'pipe'] })` with `MCC_AGENT_FD_OUT=3`, `MCC_AGENT_FD_IN=4` works on both systems.
- If a descriptor cannot be opened, the client logs one line ("Agent bridge could not start") and goes on
  without the bridge.
- The bot needs `InventoryHandling = true` in the client config for window and inventory events. Without
  it the bridge still reports chat, and says so once: `err {op: "init", reason: "inventory_disabled"}`.
- Chat and commands keep going through stdin, exactly as today. The host builds every command line from the
  profile's templates (`/msg`, `/bal`, `/takas`, ...); the bridge has no way to send text.

## Framing

- One JSON object per line, UTF-8, `\n` terminated, in both directions.
- Events: at most 256 KiB per line. An event that would be larger is replaced by
  `err {op: "emit", reason: "too_large", event}`.
- Operations: at most 4 KiB per line. A longer line is skipped unread and answered with
  `err {op: "?", reason: "too_long"}`. Empty lines are ignored; a trailing `\r` is ignored.
- Every event starts with `e` (name), `seq` and `at`:
  - `seq`: 1, 2, 3, ... per process, never reused, also across reconnects. A new process starts again at 1,
    and the host tells processes apart itself (its own `boot`).
  - `at`: Unix time in milliseconds.
- The client never blocks on the host: events wait in a queue of 4096 lines. When it is full, events are
  dropped. A dropped event still uses its `seq`, so a gap in `seq` means loss, and once the queue drains
  the client sends `err {op: "emit", reason: "overflow", dropped}`. After a loss the host asks for snapshots
  (`snapshot`) before it trusts its view of windows or the inventory.
- Unknown fields must be ignored by both sides. New events may be added in a later version; a host ignores
  event names it does not know.
- If the host closes the event descriptor, the client stops writing events and goes on as a plain bot.

## Events (client → host)

| Event | Fields | When |
| --- | --- | --- |
| `hello` | `v` (contract version), `pid` | once per process, when the channel opens |
| `joined` | `user`, `protocol` | the bot joined the game (also after every reconnect) |
| `left` | `reason` (`ConnectionLost`, `InGameKick`, `LoginRejected`, `UserLogout`) | the connection ended |
| `chat` | `type`, `text`, `clicks` | every chat, system and action bar line |
| `players` | `list` | the tab list changed (checked once a second, at most 200 names) |
| `win_open` | `id`, `type`, `title`, `size`, `total` | the server opened a window |
| `win_items` | `id`, `ws`, `state`, `items` | the window's full content (first content, a big change, or a snapshot) |
| `win_slot` | `id`, `ws`, `state`, `slot`, `item` | up to 12 changed slots of the open window, one event each |
| `win_close` | `id`, `by` | the window closed |
| `inv` | `held`, `items`, `cursor` | the player inventory changed (at most every 250 ms) or a snapshot |
| `held` | `slot` | the selected hotbar slot changed |
| `pos` | `x`, `y`, `z`, `dim` | moved by a block or more, or changed dimension (checked once a second) |
| `ack` | `id`, `op` | an operation was carried out (sent to the server) |
| `err` | `id?`, `op`, `reason`, ... | an operation was refused, or something went wrong |

### `chat`

```json
{"e":"chat","seq":41,"at":1792844160123,"type":"system","text":"✉ »» Karsi: selam","clicks":[["suggest_command","/m Karsi "]]}
```

- `type`:
  - `system`: a server message (the system chat packet). Plugins' messages, including the SMP's PMs
    (`pm.sk`) and public chat (the SMP re-sends player chat as system messages), arrive as `system`.
  - `player`: a player's chat line as the game sends it (signed or unsigned player chat, and
    "profileless" chat that carries a chat type and a sender name).
  - `actionbar`: text above the hotbar.
- `text`: what the player sees, without formatting codes. Every line break (`\n`, `\r\n`, `\r`, U+000B,
  U+000C, U+0085, U+2028, U+2029) is replaced by `⏎`, so `text` is always one line and anchored patterns
  (`^…$`) mean what they say. At most 2048 characters.
- `clicks`: the click actions of the line's components, in reading order (sender, then content), each
  `[action, value]`, at most 16, duplicates removed, values cut at 256 characters (line breaks as `⏎`).
  Actions are the game's: `run_command`, `suggest_command`, `open_url`, `copy_to_clipboard`, `change_page`,
  `show_dialog`, `custom`. The value is the command, URL, text or page. A click applies to its component
  and its children; the list does not say which part of the text carries it.
- Hover texts are not reported.
- Lines the console hides by configuration (`ShowSystemMessages`, `ShowXPBarMessages`) are still reported.

What the host can rely on (the SMP's PM rule, AFK+ design §2.11): a player cannot add a click to text
they type there (`pm.sk` escapes tags, public chat is a plain text component), and a player's own chat
can never arrive as type `system` with the PM's `suggest_command` click. The text alone proves nothing.

### Windows

```json
{"e":"win_open","seq":57,"at":1792844170001,"id":3,"type":"generic_9x6","title":"ᴛᴀᴋᴀs - Karsi","size":54,"total":90}
{"e":"win_items","seq":58,"at":1792844170003,"id":3,"ws":57,"state":1,"items":[{"slot":0,"id":"minecraft:black_stained_glass_pane","count":1,"cnames":["minecraft:custom_name"],"components":{},"name":" ","fp":"…"}]}
{"e":"win_slot","seq":61,"at":1792844171200,"id":3,"ws":57,"state":4,"slot":16,"item":{"slot":16,"id":"minecraft:elytra","count":1,"cnames":[],"components":{},"fp":"…"}}
{"e":"win_close","seq":70,"at":1792844180000,"id":3,"by":"server"}
```

- One window at a time, as in the game: the last one the server opened. Operations only act on it.
- `id`: the server's window id. Ids are reused (they cycle 1..100, and start again on every connection).
- `ws`: the `seq` of the `win_open` this content belongs to. It identifies one opening of a window; every
  window operation must carry it (see Operations). A server that changes a window's title re-sends the
  window: a new `win_open` with the same `id` and a new `seq` follows, then its content. TradeSystem does
  this during its countdown (`%tradesystem_countdown_fancy%` in the title).
- `type`: the game's menu name: `generic_9x1` … `generic_9x6`, `generic_3x3`, `crafter_3x3`, `anvil`,
  `beacon`, `blast_furnace`, `brewing_stand`, `crafting`, `enchantment`, `furnace`, `grindstone`, `hopper`,
  `lectern`, `loom`, `merchant`, `shulker_box`, `smithing`, `smoker`, `cartography_table`, `stonecutter`,
  `unknown`.
- `size`: the window's own slots (54 for a 6-row chest, 3 for an anvil). `total`: with the player inventory
  section that follows them (36 slots: 27 main, then 9 hotbar), so slot `size + 27 + n` is hotbar slot `n`.
- `title`: plain text, line breaks as `⏎`, at most 256 characters. Anyone who can name a window can put
  anything here: match layouts, not titles.
- `state`: the server's state id for the window at that moment (informational).
- `items` lists only non-empty slots, sorted. `win_slot` with `item: null` means the slot became empty.
- `by`: `server` (the server closed it), `client` (a `close` operation), `replaced` (another window opened).
- Content first arrives as `win_items`. Later changes arrive as `win_slot` events, or as one `win_items`
  when more than 12 slots changed at once (a full resync).

### `inv`

```json
{"e":"inv","seq":80,"at":1792844181000,"held":0,"items":[{"slot":36,"id":"minecraft:elytra","count":1,"cnames":[],"components":{},"fp":"…"}],"cursor":null}
```

- Player inventory window slots: 0 crafting result, 1–4 crafting grid, 5–8 armor, 9–35 main, 36–44 hotbar,
  45 off hand.
- `cursor`: the item held on the mouse cursor (after a `pickup` click), or null. The client follows the
  server's word on it (the 1.21.2+ cursor packet and the carried item of a full resync), not only its own
  prediction.
- `held`: the selected hotbar slot, 0–8.

## Items

```json
{"slot":16,"id":"minecraft:elytra","count":1,"name":"Kanat","itemName":"…","lore":["…"],
 "ench":{"minecraft:unbreaking":3},"stored":{"minecraft:sharpness":5},"damage":300,"maxDamage":432,
 "cnames":["minecraft:damage","minecraft:enchantments"],"removed":["minecraft:glider"],
 "components":{"minecraft:custom_data":{…},"minecraft:block_entity_data":{"type":9,"nbt":{…}}},
 "fp":"3f9c…","unparsed":true}
```

| Field | Meaning | Trust |
| --- | --- | --- |
| `id` | registry id of the item type | real data |
| `count` | stack size | real data |
| `ench` | enchantments from the `minecraft:enchantments` component, `{registry id: level}` | real data |
| `stored` | stored enchantments of an enchanted book (`minecraft:stored_enchantments`); never active | real data |
| `damage` | the `minecraft:damage` component (absent: 0) | real data |
| `maxDamage` | only when the stack overrides it (`minecraft:max_damage`); otherwise the item type's default applies, which the host keeps in its catalog (elytra 432, ...) | real data |
| `removed` | default components the stack removes (`!minecraft:glider` makes an elytra that cannot fly) | real data |
| `cnames` | every component the stack carries, in network order | real data |
| `components` | decoded values of `custom_data`, `entity_data`, `block_entity_data` (`type` = registry id + `nbt`), `profile` (`name`), `repair_cost`, `unbreakable`, `container` and `bundle_contents` (`count` and, one level deep, `items`) | real data |
| `name` | custom name (`minecraft:custom_name`), plain text | player-made (anvil) |
| `itemName` | item name override (`minecraft:item_name`) | server or plugin text |
| `lore` | lore lines, plain, at most 32 | anyone's text |
| `fp` | 32 hex digits of SHA-256 over the id, count, every component's network bytes and the removed list | real data |
| `unparsed` | true when something could not be reported faithfully: an unknown item type, an enchantment the client does not know (a server's custom enchantment), NBT cut for size or depth, containers nested more than one level | — |

Rules for the host (AFK+ design §2.4): match the item by `id` and components, never by `name` or `lore`;
refuse `unparsed` items, items with `removed` entries, containers (`*shulker_box`, `bundle`, `chest`) and
components a catalog entry does not allow. `fp` changes whenever anything about the stack changes, so a
check made on one `fp` holds only while the slot keeps it.

NBT is written as plain JSON: compounds as objects (keys sorted), lists and arrays as arrays, numbers as
numbers. A tree larger than 2000 nodes or deeper than 16 is cut (cut parts are `null`) and the item is
`unparsed`. Versions before 1.20.5 have no components: `name`, `lore`, `damage`, `ench`, `stored` come
from the NBT, which is also given as `nbt`.

If the client cannot decode a window's packet at all (a component it does not know), the connection
fails (`left`); no partial window is reported.

## Operations (host → client)

```json
{"op":"click","id":"d17-4","win":3,"ws":57,"slot":3,"button":0,"mode":"pickup"}
{"op":"rename","id":"d17-5","win":4,"ws":63,"text":"37500"}
{"op":"close","id":"d17-9","win":3,"ws":57}
{"op":"hold","id":"d17-10","slot":0}
{"op":"snapshot","id":"d17-11","what":"inv"}
```

| Op | Fields | Does |
| --- | --- | --- |
| `click` | `win`, `ws`, `slot`, `mode`, `button` | a window click: `pickup` (button 0 left, 1 right) or `quick_move` (shift-click; button 0 or 1) |
| `rename` | `win`, `ws`, `text` | types `text` as the anvil's item name (the game's "rename item" packet) |
| `close` | `win`, `ws` | closes the window |
| `hold` | `slot` (0–8) | selects a hotbar slot; answered by `ack` and a `held` event |
| `snapshot` | `what`: `inv` or `win` | re-sends `inv`, or `win_items` of the open window |

- `id` (optional, 1–64 of `A-Z a-z 0-9 _ - . :`) is echoed in the answer. Every operation gets exactly one
  answer: `ack {id, op}` or `err {id, op, reason}`. `ack` means the packet was sent; what the server did
  shows in the window events that follow (a plugin may refuse a click and resync the slot).
- Window operations (`click`, `rename`, `close`) act only on the open window, and only when `win` and `ws`
  are those of its last `win_open`, the operation arrived after that `win_open` was written, and the window's
  content was already reported. This keeps an operation meant for one window from landing in another that
  reuses its id.
- `slot`: 0 ≤ slot < `total`. Nothing else: no drops, no clicks outside the window (-999), no number-key
  swaps, no creative actions.
- `rename`: the open window must be an `anvil`; `text` must be 1–12 ASCII digits (a money amount). The anvil
  then shows the named item in slot 2; clicking slot 2 (`pickup`) takes it, which is how TradeSystem reads
  the amount.
- Rate: at most 10 operations per second (a token bucket of 10); more are refused with `rate_limited`.
- Operations are carried out on the game thread, at most 4 per tick (50 ms), in arrival order.
- Operations that arrived before the current session joined the game (a reconnect) are dropped with
  `stale`; operations waiting while the bot is not in game are dropped the same way.

### `err` reasons

| Reason | Meaning |
| --- | --- |
| `bad_request` | not JSON, not an object, a missing or mistyped field, a number out of range, or a window operation without `win`/`ws` |
| `bad_id` | `id` is not 1–64 allowed characters |
| `unknown_op` | an `op` this version does not have |
| `too_long` | the line was longer than 4 KiB |
| `busy` | more than 64 operations waiting |
| `rate_limited` | more than 10 operations in a second |
| `stale` | from before the current session or before the window opened |
| `no_window` | no window is open |
| `wrong_window` | `win`/`ws` are not the open window's |
| `not_ready` | the window's content was not reported yet |
| `bad_slot` | slot out of range |
| `bad_mode`, `bad_button` | mode or button not allowed |
| `not_anvil` | `rename` on a window that is not an anvil |
| `bad_text` | `rename` text is not 1–12 digits |
| `send_failed` | the connection could not take the packet |
| `inventory_disabled` (`op: "init"`) | the client runs without inventory handling |
| `overflow`, `too_large` (`op: "emit"`) | events were dropped / one event was too big |

## TradeSystem, step by step

What the trial (23 Oct 2026) saw with TradeSystem 2.6.3 Hotfix-9 on a local Canvas 26.3 server, with a
54-slot layout. Slots of the bot's own view: own money icon 3, partner money icon 5, own status 12, partner
status 14, cancel 22, own trade slots 9, 10, 18, 19, 27–29, 36–39, 45–48, partner trade slots 16, 17, 25, 26,
33–35, 41–44, 50–53; the rest is glass. Clients: 26.3, and 26.1 and 26.2 through ViaBackwards (window, anvil and
amount the same).

Every icon click replaces windows, and ids change: one purchase went trade window 1 → anvil 2 → trade window 3,
and the countdown re-opened window 3 three times. Take `win` and `ws` from the latest `win_open` every time.

Purchase (the bot pays):

1. stdin `/takas <partner>`; the partner accepts; `win_open` (`generic_9x6`, size 54) and `win_items`: `"$: 0"`
   on 3 and 5, `"Durum: Hazır değil"` on 12 and 14.
2. The partner's items arrive as `win_slot` events in partner slots.
3. `click` slot 3 (`pickup`, button 0): `win_close {by: "server"}`, `win_open` (`anvil`, size 3), `win_items`
   (slot 0: a paper named `"0"`).
4. `rename` `"37500"`: `win_slot` 0 and 2, a paper named `"37500"`.
5. `click` slot 2: `win_close {by: "replaced"}`, `win_open` (the trade window, new id), `win_items`; slot 3 now
   reads `"$: 37K"` (see below).
6. Check the partner slots against the deal, then `click` slot 12: slot 12 turns `"Durum: Hazır"`. When the partner
   is ready too (slot 14), the countdown re-opens the window with ` - 3s`, ` - 2s`, ` - 1s` titles (a new `ws`
   each), then `win_close {by: "server"}` and `chat` `"Takas> Takas tamamlandı."`, `"  + 1x Elytra"`,
   `"  - 37K $"`.
7. The proof is the `/bal` difference (in the trial 100,000 → 62,500) and the `inv` difference, not the chat.

- An amount above the balance: after step 5 `chat` `"Takas> Bakiye yetersiz! En fazla 3M $ verebilirsiniz."`
  and the anvil stays open; close it.
- A change of the partner's offer drops the bot's ready (slot 12 back to `"Durum: Hazır değil"`).

Sale (the bot gives items): `click` the item in the inventory section (`pickup`; `inv.cursor` shows it), then
`click` an own trade slot. The partner types its amount in its own anvil; the bot sees it only on slot 5.

### Amounts are shown rounded down

TradeSystem shows amounts from 1,000 in whole thousands and from 1,000,000 in whole millions, rounded down:
999 → `"$: 999"`, 1,600 → `"$: 1K"`, 37,999 → `"$: 37K"`, 1,500,000 → `"$: 1M"`. Its finish report and its own
log use the same form. So:

- the bot's own amount is exact because the bot types it (the host lets only the deal amount through `rename`);
  the icon only confirms the range;
- the partner's amount can be read only to its unit: 7,000 and 7,200 give the same item, name and `fp`. When the
  bot sells, the deal price must be one the icon can prove: below 1,000 any amount, from 1,000 whole thousands, from
  1,000,000 whole millions, and slot 5 must show exactly that number (`"$: 7K"` for 7,000). Rounded down, that
  means the partner pays at least the price. The `/bal` difference after the trade stays the exact proof.

### Completion depends on the server's thread checks

TradeSystem 2.6.3 Hotfix-9 closes both inventories at the end of the countdown from Folia's global region. On
Canvas with `guard-severity: THROW` that throws inside TradeSystem's task: the countdown ends and nothing else
happens, the trade stays open and nothing moves (cancelling still works). With `LOG` the same moment logs
"Cannot close container async" four times and the trade completes. A host treats "countdown over, no finish
line, window still open" as a stuck trade: wait at most a few seconds, cancel, check `/bal` and `inv`.

## Stdout bridge hardening (`@@MCC@@`)

The launcher bridge (`LauncherBridge.cs`, `MCC_LAUNCHER_BRIDGE=1`) prints events on stdout as lines starting
with `@@MCC@@`. Before 4.1.0 a server could forge such a line: a kick message or a window title is logged with
its line breaks, so `afkc-kick\n@@MCC@@{"e":"joined",...}` produced a line that started with the prefix (the
trial shows it on the released 4.0.1 client). Chat lines were already safe (their line breaks became spaces).

From this version:

- **One line per write** when `MCC_LAUNCHER_BRIDGE` is set: every line break inside one console write becomes
  `⏎`, so no server text can start a stdout line of its own. Multi-line console output (help, kick messages
  with several lines) shows on one line. This alone closes the hole for every host.
- **Nonce**: when the host sets `MCC_BRIDGE_NONCE` (16–128 of `A-Z a-z 0-9 _ -`; any other value is ignored),
  every event line is `<nonce>@@MCC@@{json}`. A host that sets a nonce must accept only lines that start with
  `<nonce>@@MCC@@` from a client of this version or newer, and treat everything else as text. Clients before
  this version ignore the variable and print the bare prefix, so a host decides by the client version it runs
  (the AFK client version from the content index), not by the first line it sees.
- The nonce never leaves the host and the client: the client does not print it except as the prefix, and
  never sends it to a server.

## Host notes (`afk-host`)

- Give the child exactly two extra descriptors and nothing else: the write end of the event pipe as fd 3 and
  the read end of the operation pipe as fd 4, both without `FD_CLOEXEC` (Rust's `std::process::Command` has no
  option for this: `pre_exec` with `dup2`, or the `command-fds` crate). Close the parent's copies of the child
  ends after spawning, so EOF works both ways.
- Read fd 3 continuously on its own task; the client drops events rather than wait for a slow reader.
- A bot restarted by the host is a new process: `seq` starts again at 1. Keep the host's own `boot` id per spawn
  and drop any state (open window, `ws`, cursor) of the old process.
- Every window operation needs the `win` and `ws` of the window the host checked; take them from the latest
  `win_open`. After `err` `wrong_window` or `stale`, re-read the window (`snapshot` `win`) before trying again.
- The last gate (`agentgate.rs`) decides what may be sent; the bridge repeats the shape checks above as a second
  line, not instead of it.

## Compatibility

- Without the two descriptor variables nothing of the agent bridge runs, and the only visible change for other
  users is the stdout hardening above (only with `MCC_LAUNCHER_BRIDGE`).
- Other changes the bridge needed, active for every user:
  - the client follows the server's cursor item (1.21.2+ cursor packet, carried item of a full resync);
  - an item's removed components are kept (`Item.RemovedComponents`);
  - when a server sent its own enchantment registry, an id it does not name as a vanilla enchantment is
    reported as unknown instead of the vanilla enchantment that has that id by default;
  - bots get every chat line through `ChatBot.OnChatReceived(text, message)`, with `message.isOverlay`,
    `message.isProfileless` and (while the agent bridge runs) `message.clicks`;
  - `McClient.SendRenameItemInWindow(windowId, name)` names the item in a given anvil window.
