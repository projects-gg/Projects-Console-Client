# Agent bridge (AFK+)

The agent bridge is an out-of-band channel between this client and the AFK+ host (`afk-host` on the cloud
machine, which relays to `afk-agent`). It reports what a trade needs to see (chat lines with their click
actions, windows and their items, the player inventory) and takes the few window actions a trade needs (a
click, an anvil name, closing a window). It is the third gate of AFK+'s safety model: it only takes
structured window operations, never a text command.

Code: `MinecraftClient/ChatBots/AgentBridge.cs` (events and operations),
`AgentBridgeChannel.cs` (descriptors, framing, queues), `AgentOperation.cs` (operation parsing),
`AgentBridgeItems.cs` (items), `Protocol/Message/ChatClick.cs` (click actions of chat components),
`ConsoleKinds.cs` and `AutoRespond.cs` (version 2's console kinds and strict values).
Tests: `MinecraftClient.Tests/AgentBridgeTests.cs`, `AgentBridgeV2Tests.cs`, `ConsoleKindsTests.cs`,
`AutoRespondVarsTests.cs`.

Contract version: **2** (the `v` of `hello`): version 1 as described below, and the additions in
[Version 2](#version-2-client-411). A host that asks for movement (`MCC_AGENT_MOVE=1`) gets **3**: version 2 and
[Version 3](#version-3-client-420-movement).

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
  are those of its last `win_open`, the operation arrived after that `win_open` was written, and (`click` and
  `rename`) the window's content was already reported. This keeps an operation meant for one window from landing
  in another that reuses its id. `close` does not wait for the content (from 4.1.1): a window the server sent but
  never filled can still be closed.
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
| `not_ready` | the window's content was not reported yet (`close` does not wait for it) |
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

## Version 2 (client 4.1.1)

For the cloud features (live inventory, console kinds, reply placeholders). Each part starts only with the variable
`afk-host` sets for it; without them the client behaves as version 1, apart from `hello.v` (2) and `close`, which no
longer waits for the window's content (below).

### Event groups (`MCC_AGENT_EVENTS`)

- Comma-separated groups: `chat` (`chat`), `players` (`players`), `inv` (`inv`), `win` (`win_open`, `win_items`,
  `win_slot`, `win_close`), `held` (`held`), `pos` (`pos`). Only the named groups are written; the others are not
  even turned into JSON and use no `seq`. `hello`, `joined`, `left`, `ack` and `err` are always written.
- Unset: every group, as in version 1. Unknown names count for nothing; set but empty: no group.
- Without `chat` the client collects no click actions. Without `win` windows are still tracked (player inventory
  operations wait for a window to close), but no window operation can name one.
- `snapshot` of a group that is not written: `err` `masked`.

### Windows for a live view

- `win_open` gains `titleRaw`: the title's text component as the server sent it, on one line. From 1.20.4 it is the
  component's NBT tree written as JSON (compounds as objects with sorted keys, lists as arrays, numbers as numbers, a
  plain string root as a JSON string); before, its JSON text written compactly (text that is not JSON becomes a JSON
  string). Fonts (`font`), colors and every glyph are kept, so a custom menu whose background is drawn with resource
  pack glyphs can be recognised. `title` stays the plain text, cut at 256 characters; it keeps Private Use Area
  characters (U+E000–U+F8FF), with which ItemsAdder draws menu backgrounds, as they are (only formatting codes and
  line breaks change). `titleRaw` is left out when it is larger than 32 KiB or the tree had to be cut.
- With `MCC_AGENT_EVENTS` set (a version 2 host), window content is reported once a tick (50 ms): a burst of slot
  packets becomes one `win_items` or `win_slot` change.
- After the bot's own `click` or `drop` (once acked), the window (`win_slot`) and the player inventory (`inv`) are
  reported when the server's answer had 150 ms to arrive, also when the server sends none: a vanilla server confirms
  a correctly predicted click with silence, and then the client's view is the result. A plugin that refuses a click
  resyncs the slot, and that change follows as usual.
- A `snapshot` answers at once with the client's view, which right after a click is the client's guess; the `win_slot`
  changes that follow are against what the snapshot said (from 4.2.1; before, a snapshot taken between a click and the
  server's answer hid a refused click's resync, and the slot looked empty until the window opened again).
- Without `MCC_AGENT_EVENTS` the timing is version 1's: an event for every packet, nothing after a move the server
  does not answer.
- `close` is accepted before the window's content was reported, for every host. On Canvas (Folia) with
  `guard-severity: THROW`, a menu opened from the wrong region thread (a console command, a global-region task) fails
  after the window was sent: the client gets `win_open` and never `win_items`. A player closes such a window with
  Esc; the bot now can too (the trial of 24 Oct 2026 on a local Canvas 26.3 saw exactly this). Clicks, drops and
  `rename` still wait for the content (`not_ready`).

### The player inventory

- `click` and `drop` with `win: 0, ws: 0` act on the player inventory while no window is open: slots 5–45 (5–8 armor,
  9–35 main, 36–44 hotbar, 45 off hand). Any other slot is `bad_slot`.
- Refused with `wrong_window` while a window is open (its own slots cover the player inventory then: `size + n`),
  `inventory_disabled` without inventory handling, `not_ready` until the session's first `inv` was written, and
  `stale` when the operation arrived before that `inv`.
- `ws: 0` is accepted only with `win: 0` on `click` and `drop`. `rename` and `close` never take `win: 0`: as in
  version 1, they answer `no_window` or `wrong_window`, and their `ws` must be positive.

### `drop`

```json
{"op":"drop","id":"d1","win":0,"ws":0,"slot":12,"stack":false}
```

- A window click in mode 4 (throw): `stack` false or absent throws one item (button 0), true the whole stack
  (button 1), in the open window (its `win` and `ws`, as `click`) or in the player inventory (`win: 0, ws: 0`).
- Never slot −999: nothing is thrown from the cursor. `bad_state` while the cursor holds an item; `empty_slot` when
  the client sees no item in the slot. Within the general rate (10 a second).

### `use`

```json
{"op":"use","id":"u1"}
```

- The use-item packet for the main hand (an item that opens a menu, food, ...). To use another hotbar slot, `hold`
  it first.
- At most one a second (`rate_limited`); `window_open` while a window is open.

### Items

| Field | Meaning | Trust |
| --- | --- | --- |
| `potion` | the potion of `minecraft:potion_contents` as a registry id (`"minecraft:strong_swiftness"`); before 1.20.5 the NBT's `Potion`. A potion number the client does not know makes the item `unparsed` | real data |
| `color` | `minecraft:dyed_color`, else the potion's `custom_color` (before 1.20.5: `display.color`, else `CustomPotionColor`), an RGB integer (0–16777215) | real data |
| `cmd` | custom model data: from 1.21.4 the first of its `floats` floored to an integer (only when finite and within a 32-bit int), 1.20.5–1.21.3 the component's int, before 1.20.5 the NBT's `CustomModelData` | server data (a number, not text) |
| `model` | the `minecraft:item_model` component (1.21.2+), only when the stack's patch carries it | server data (an id, not text) |

`cmd` and `model` are what item art maps a server's custom items by; nested items (`container`, `bundle_contents`)
carry them too.

Text fields and click values that carry ill-formed UTF-16 (a lone surrogate) get U+FFFD in its place.

### Console kinds (`MCC_CONSOLE_KINDS=1`)

Only together with `MCC_LAUNCHER_BRIDGE=1` and a valid `MCC_BRIDGE_NONCE`. Every console write to stdout is then one
event line; the client says where it saw the line and the host decides what kind it is:

```text
<nonce>@@MCC@@{"e":"line","src":"system","text":"✉ »» Kaan_07: selam","guess":"pm","who":"Kaan_07"}
```

| Field | Meaning |
| --- | --- |
| `src` | `system` (a system message), `player` (a player's chat packet), `overlay` (the action bar), `title`, `subtitle`, `client` (the client's own line), `error` (its logger's errors) |
| `text` | as the console would print it: no formatting codes, one line (breaks as `⏎`), at most 8192 UTF-16 code units (a cut text ends with `…`). Chat lines carry the message without the client's signature marks (`▌`) |
| `ct` | `player` only: the chat type's registry id (1.19+, when the server's registry named it), such as `minecraft:msg_command_incoming` |
| `who` | `player`: the sender (its tab list name, else the display name), for `minecraft:msg_command_outgoing` the recipient; `system` with `guess`: the parse's sender |
| `guess` | `system` only: the client's own parse, in its auto-replies' order: `chat` (`IsChatMessage`, with `[ChatFormat]` `Public`), else `pm` (`IsPrivateMessage`, with `Private`) |
| `st`, `ev` | client 4.2.4: the line's looks, clicks and hovers ("Client 4.2.4: console line looks") |

- `ShowSystemMessages` and `ShowXPBarMessages` hide nothing from these events, and titles, which the console never
  printed, are events too. Chat links (`ShowChatLinks`) are `client` lines. stderr and the other bridge events are
  unchanged.
- No server text can make an event: every write is one stdout line and the server never learns the nonce.

Boss bars, from the boss bar packet:

```text
<nonce>@@MCC@@{"e":"boss","op":"add","id":"4f1c7e5a-0d2b-4c39-9a51-8f2b6d0e7c11","text":"Etkinlik","progress":0.25,"color":"yellow"}
```

- `add` (text, progress, color), `update` with what changed (`progress`, `text` or `color`), `remove`. A change of
  the bar's flags is not reported.
- `id` the bar's UUID; `text` plain, at most 256 characters; `progress` 0–1 (four decimals); `color` the game's name
  (`pink`, `blue`, `red`, `green`, `yellow`, `purple`, `white`).

### Strict values (`MCC_MATCH_VARS=strict`)

- A `[Match]` block's `vars=` (`u` and `1`–`9`, comma-separated) names the values its action uses: the sender and
  the pattern's groups. Each must be 1–24 characters, each `_` or a letter or number (general category L or N) of the
  Basic Multilingual Plane that is not a default ignorable code point (the Hangul fillers U+115F, U+1160, U+3164 and
  U+FFA0). A missing value fails too: a group that took no part, `u` on a line without a sender.
- When one fails, the rule does not answer that line and its cooldown does not start; the client writes at most one
  console line a minute per rule (`[AutoRespond] Reply not sent: …`).
- The action's script (`script <file>`) gets exactly the checked values, as `%match_u%`, `%match_1%`, ..., and no
  other variable. (AutoRespond's variables never reached a script started that way before.)
- A `send` line of that script that its values make longer than one chat message (256 characters, or the server's
  limit) is not sent (`[Script] Reply not sent: …`): a value's length never chooses where a long line is split.
- A block whose `vars=` names anything else, or whose pattern has a named group, is not loaded at all (.NET numbers
  named groups after the others; `afk-host` writes them unnamed).
- Without the variable, `vars=` is ignored; blocks without `vars=` run as before.

### New `err` reasons

| Reason | Meaning |
| --- | --- |
| `masked` | `snapshot` of a group the host did not ask for |
| `empty_slot` | `drop` on a slot the client sees empty |
| `bad_state` | `drop` while the cursor holds an item |
| `window_open` | `use` while a window is open |

Release builds are not ReadyToRun yet: that waits for the memory measurement.

## Version 3 (client 4.2.0): movement

For the cloud's movement (projects-studio `services/cloud-afk/README.md`, "Movement"; `services/afk-host/README.md`,
"Movement"): the player walks the account by hand from the site, the launcher or the phone. Everything here starts only
with `MCC_AGENT_MOVE=1`; without it the operations below are `unknown_op` and no event changes, exactly as in version 2.

| Variable | Meaning |
| --- | --- |
| `MCC_AGENT_MOVE` | `1`: `hello.v` is 3, the movement operations are known, and `pos` and `move` are written whatever `MCC_AGENT_EVENTS` names (an unset `MCC_AGENT_EVENTS` keeps version 1's groups and timing, so an AFK+ bot's trade proof is unchanged) |
| `MCC_AGENT_FOLLOW` | the names `approach` and `follow` may name, comma-separated (player names; others are ignored). Empty or unset: none |

The client moves only with terrain and entity handling (`TerrainAndMovements`, `EntityHandling`); without them every
movement operation but `stop` is refused with `movement_disabled`.

### Operations

```json
{"op":"step","id":"m1","dir":"forward","n":1}
{"op":"jump","id":"m2"}
{"op":"sneak","id":"m3","on":true}
{"op":"goto","id":"m4","x":120,"y":64,"z":-40}
{"op":"approach","id":"m5","name":"Kaan_07"}
{"op":"follow","id":"m6","name":"Kaan_07"}
{"op":"stop","id":"m7"}
```

| Op | Fields | Does |
| --- | --- | --- |
| `step` | `dir`, `n` (1–8) | walks `n` blocks in a straight line toward `dir`: `forward`, `back`, `left`, `right` of where the account looks (snapped to a compass direction), or `north`, `south`, `east`, `west`. It climbs a block, steps down, and stops before a wall, a drop of more than 3 blocks or a harmful block. Afterwards the account looks where it looked before its steps, so `forward` keeps its meaning. A step toward the direction it is already stepping extends the walk, to at most 8 blocks ahead of where it stands |
| `jump` | | one jump, when it stands on something |
| `sneak` | `on` | sneaks until `sneak` off: the server sees the sneak key (Player Input from 1.21.6, Player Command before), and the physics move slowly and never off an edge. While it sneaks, nothing else (AntiAFK, a slab overhead) toggles it |
| `goto` | `x`, `y`, `z` (block; `x`, `z` within ±30,000,000, `y` within ±2048) | walks to that block, at most 256 blocks away (from the feet to the block's centre), within 2 minutes |
| `approach` | `name` | walks to 2 blocks from that player and stops, within 2 minutes |
| `follow` | `name` | keeps 2–3 blocks behind that player, at most 30 minutes, while it stays in sight |
| `stop` | | stops steps, a walk or following (sneaking stays) |

- **One activity at a time.** `step`, `goto`, `approach` and `follow` replace what runs (no `move` event in between);
  `jump` and `sneak` go with it. There are no lists of waypoints, no repetition, no patrol.
- **`stop` always works**: it is never `busy` (it gets in while twice the queue waits), never `stale`, never rate
  limited; it stops movement at the next tick, ahead of the operations waiting before it, which it makes `stale`. Before
  joining, or without movement, it is acknowledged and does nothing.
- **The route.** `goto`, `approach` and `follow` search a path through the terrain the server sent (never into a chunk
  it did not send): a weighted A* over block cells with MCC's moves (`Movement.GetAvailableMoves`, no unsafe cells),
  of at most 4,000 cells (1,500 while following), on another thread, cancelled after a second, one at a time. When the
  goal is out of reach of one search (unknown terrain, the limit), the account walks to the cell nearest to it and
  searches again; three searches that bring it no nearer end the walk (`blocked`). Following searches again at most once
  a second, when the player moved more than 2 blocks or the walk ended. A walk that does not move for 2 seconds counts
  as stuck. A step searches along its own line only (128 cells), on the game thread.
- **Names.** `approach` and `follow` take only a name in `MCC_AGENT_FOLLOW` and never the account's own (`not_allowed`),
  and only a player the client tracks within 64 blocks (`not_visible`): the host decides whose name it may pass, the
  client refuses anything else.
- **Rate.** Movement operations other than `stop` take from a bucket of 8 that gets 4 back a second (`rate_limited`),
  besides version 1's 10 a second for every operation.
- `ack` means the activity started; how it goes shows in `move` events.

### Events

With `MCC_AGENT_MOVE=1`, `pos` (still checked once a second) carries `facing` and is written whenever the block the
feet are in, the dimension or `facing` changed (a one-block step may move the feet less than a block); `move` says what
the account does, on every change:

```json
{"e":"pos","seq":90,"at":1792844190000,"x":120.5,"y":64.0,"z":-39.5,"dim":"minecraft:overworld","facing":"north"}
{"e":"move","seq":91,"at":1792844190020,"doing":"goto","id":"m4","to":{"x":120,"y":64,"z":-40},"until":1792844310020,"sneak":false}
{"e":"move","seq":97,"at":1792844199000,"doing":"idle","ended":"arrived","sneak":false}
```

| Field | Meaning |
| --- | --- |
| `pos.facing` | `north`, `south`, `east`, `west`: what `forward` means now (while it takes steps, where it looked before them) |
| `doing` | `idle`, `step`, `goto`, `approach`, `follow`; right after `joined`, `idle`, or `off` when the client runs without terrain or entity handling (not started with them, or the joined version has none) |
| `id` | the operation that started it |
| `to` | `goto`: the target block |
| `name` | `approach`, `follow`: the player |
| `until` | `goto`, `approach`, `follow`: when the client stops it by itself (Unix milliseconds) |
| `ended` | with `idle`: `arrived`, `blocked` (a wall, a drop, stuck, no way further), `no_path` (no way there found), `timeout` (a walk not there in 2 minutes), `limit` (following stopped after 30 minutes), `out_of_sight` (the player left sight), `reset` (the server moved the account: a respawn, another world, a jump of more than 8 blocks in a tick); none after a `stop` |
| `sneak` | it sneaks |

A new session starts idle and not sneaking; a disconnection ends what ran without a `move` event (`left` says it).

### New `err` reasons

| Reason | Meaning |
| --- | --- |
| `movement_disabled` | no terrain or entity handling |
| `not_ready` | the server has not told the client where it stands yet |
| `bad_dir`, `bad_count`, `bad_name` | a `step` direction, a step count outside 1–8, a name that is no player name |
| `blocked` | a `step` with not one block to go |
| `too_far` | a `goto` more than 256 blocks away |
| `not_allowed` | a name not in `MCC_AGENT_FOLLOW`, or the account's own |
| `not_visible` | that player is not in sight (64 blocks) |

## Client 4.2.3: heads and styled text

For the cloud's inventory view (projects-studio `services/cloud-afk/README.md`, "Inventory"): a player head's own skin
and names and lore in the colours the game's tooltip draws them in. Items gain four optional fields; the plain `name`,
`itemName` and `lore` stay exactly as before (trade checks read them), and a host that does not know the fields ignores
them.

```json
{"slot":3,"id":"minecraft:player_head","count":1,"skin":"8a6d…0998",
 "name":"Kılıç","nameStyle":[[5,"6lo"]],
 "lore":["Hasar +12","","Yeşil kalın sade"],"loreStyle":[[[6,"7"],[3,"c"]],null,[[6,"a"],[6,"al"],[4,"o"]]]}
```

| Field | Meaning | Trust |
| --- | --- | --- |
| `skin` | a head's skin: the hash of its texture on Mojang's texture server, from the profile's `textures` property (base64 JSON, `textures.SKIN.url`; before 1.20.5 `SkullOwner.Properties.textures`), only for `http(s)://textures.minecraft.net/texture/` and 16–64 hex digits, lower case. Any other address, a cape only or a value that does not read: none. A profile with only a name or an id (the game would look the skin up) has none | server data (an address, not text) |
| `nameStyle`, `itemNameStyle` | the look of `name` and `itemName` | server or plugin text |
| `loreStyle` | one entry a `lore` line: its look, or `null`. Left out when no line has one | anyone's text |

A look is spans over the plain text: `[[length, code], ...]`, the lengths in UTF-16 code units adding up to the text's
length. A code is a colour, one of `0`–`f` (the game's 16, as its formatting codes) or `#rrggbb`, or none (the field's
own: the rarity colour for names, dark purple for lore), followed by the flags that are on, in the order `k` obfuscated,
`l` bold, `m` strikethrough, `n` underlined, `o` italic. The spans carry no text.

- The client walks the text component itself (ChatParser's `§` string loses an `italic: false` at the root and whether
  a colour came from the component or from a code in its text): `extra` inherits, a list is its first part with the
  others after it, `translate` takes the component's look and its arguments their own over it (since 4.2.4; 4.2.3 gave
  the arguments the translation's look). Codes inside a text
  follow the game: a colour code sets the colour and turns every flag off, a flag code adds its flag, `§r` goes back to
  the text's own look.
- Lore starts italic and a custom name starts italic, as in the game; `itemName` starts with no flags.
- When the walk does not come to exactly the plain text (a line break or a surrogate pair split between two parts,
  nesting deeper than 64, ...), the field has no look. A text cut at 256 characters keeps the spans of what is left. A field whose one span is the field's own look (no colour, no flags) has none
  either; adjacent spans with the same code are one; a look of more than 64 spans is left out.

## Client 4.2.4: console line looks

For the console in the game's colours, with its hovers and clicks (projects-studio `services/cloud-afk/README.md`,
"Console and logs"): a console kinds `line` event gains two optional fields. `text` stays exactly as before and is the
only text a host matches, masks, searches or gives an AI; the fields carry no text of the line, and a host that does not
know them ignores them.

```text
<nonce>@@MCC@@{"e":"line","src":"player","text":"<FrostyOtter88> lobiye geliyorum","who":"FrostyOtter88","st":[[1,""],[13,"b",0],[18,""]],"ev":[{"click":{"a":"suggest_command","v":"/tell FrostyOtter88 "},"hover":{"entity":{"id":"minecraft:player","name":"FrostyOtter88"}}}]}
```

| Field | Meaning | Trust |
| --- | --- | --- |
| `st` | spans over `text`: `[length, code]` or `[length, code, event]`, the lengths in UTF-16 code units adding up to the length of `text`, the code as in 4.2.3 (none: the line's own colour), `event` an index into `ev` | server or player text |
| `ev` | the parts' events, at most 8, each `{"click": {"a", "v"}, "hover": {...}}` with either or both | server or player text |

- Clicks: only `run_command` (its value starts with `/`), `suggest_command`, `open_url` (`https://`, a host, no
  whitespace) and `copy_to_clipboard`, the value 1–256 code units of well-formed text without a control character (C0,
  DEL, C1), a bidirectional mark, embedding, override or isolate (U+061C, U+200E, U+200F, U+202A–U+202E, U+2066–U+2069) or
  a line or paragraph separator. A longer value is never cut (a cut command would be another command): that click is left
  out, as is every other action (`change_page`, `show_dialog`, `custom`, ...). A part's own click hides its parent's,
  also when it is one of those.
- Hovers, one of:
  - `{"text", "st"?}` (`show_text`): the text walked on its own, its parts' clicks and hovers not followed; line breaks
    as `\n`, at most 8 lines, the characters above left out, at most 256 code units (a cut text ends with `…`), at most
    16 spans without events;
  - `{"item": {...}}` (`show_item`): the item as "Items" writes one, without `slot`, `fp`, `cnames` and `components`
    (but a head's `minecraft:profile` `name`), read from the 1.20.5+ form (`id`, `count`, `components`: custom and item
    name, lore, profile, enchantments, damage, potion) with its looks; the form before (an SNBT `tag`) gives `id` and
    `count` only; at most 2048 bytes (past it the looks go, then lore lines from the end);
  - `{"entity": {"id", "name"?, "st"?}}` (`show_entity`): the type's registry id and the name (at most 64 code units),
    never the UUID.
- The client walks the components the server sent, as the game styles them: `extra` inherits, a list is its first part
  with the others after it, a translation's arguments keep their own looks over the translation's, codes inside a text
  follow the game (a colour code keeps the part's click and hover), and click and hover are inherited each on its own. A
  signed message is put together the way the client puts its text together (`ParseSignedChat`): the chat type's
  decoration with the decoration's style, else the message type's translation. A sender's name the client took from the
  tab list is its text, without events.
- `st` and `ev` are written only when that walk comes to exactly `text` (cut at 8192, the spans of what is left).
  Otherwise the looks come from the client's own formatted text (no `ev`); when that does not come to `text` either, the
  line has neither field. The client's own lines (`client`, `error`), and the titles and action bar of protocols before
  1.17, get their looks from their formatting codes.
- Caps: at most 64 spans (a line with more has neither field); at most 8 distinct events (the parts of any further one
  have no index); `st` and `ev` together at most 4096 bytes of JSON: past it the hovers go first, then the events, then
  the spans. Adjacent spans with the same code and event are one; a line whose one span has no look and no event has no
  `st`.
- Only with the console kinds on: otherwise the client keeps no component of a line.

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

## Lean mode (client 4.2.2, `MCC_LEAN`)

For many small and mostly idle clients on one machine. `MCC_LEAN=1` turns every part on, a comma separated list
(`palettes,socket`) only the parts it names; unset, nothing changes. No part changes what the client does in game or
shows; each skips work whose result nothing in a cloud profile reads:

| Part | What the client skips |
| --- | --- |
| `palettes` | The block, entity and item palettes of a feature that is off (terrain and movements, entity handling, inventory handling) are built on first use instead of at every connect, and without terrain the client no longer looks for a top slab above its head every 300 ms (with no terrain every block reads as air; the look only built the block palette). A client with the feature on builds its palette at connect, as before |
| `socket` | The packet reader no longer wakes every 10 ms to look for data: it waits on the socket (at most 100 ms, for cancellation) |
| `book` | Advancements, their tab and the recipe book (`Advancements`, `SelectAdvancementTab`, `UnlockRecipes`, `RecipeBookAdd`, `RecipeBookRemove`, the legacy statistics) are neither parsed nor kept: the `/achievement` and `/recipebook` commands and a script's `GetAchievements()` see none |

`MCC_LEAN_TRACE=1` (measurements only, never set by the host) writes one technical stderr line, with the first stack
frames, when a palette left for later is built after all.

Measured (28 Oct 2026, 26.3 on a local Canvas server, Linux container, the cloud profile; afk-host's runtime settings
`MALLOC_ARENA_MAX=2`, `DOTNET_TieredCompilation=0` with the ReadyToRun package): 32.5 MiB of anonymous, shared JIT and
kernel memory per client against 43-44 MiB for 4.2.1 with the earlier settings, linear from 1 to 30 clients; 0.67 % of
a core per idle client against 0.84 %, 60 context switches a second against 140; 0.75 CPU seconds to join against 2.0.

## Compatibility

- Without the two descriptor variables nothing of the agent bridge runs, and the only visible change for other
  users is the stdout hardening above (only with `MCC_LAUNCHER_BRIDGE`). Version 2's console kinds and strict values
  run only with their own variables (`MCC_CONSOLE_KINDS`, `MCC_MATCH_VARS`), version 3's movement only with
  `MCC_AGENT_MOVE=1`.
- Since 4.1.2, for every user: from 1.21.6 an entity action goes to the server in that version's form (sneaking is
  the Player Input packet's sneak key, the other Player Command ids moved down by two). Before, AntiAFK's "sneak" sent
  "leave bed" and "start sprinting" to 1.21.6 and later servers.
- Since 4.2.1, for every user: NBT strings (text components from 1.20.3: chat, titles, team prefixes, item names and
  lore; tag names; item NBT before 1.20.5) are read and written as Java's modified UTF-8. Before, every character
  outside the Basic Multilingual Plane (an emoji in a chat prefix) arrived as six U+FFFD, and the client wrote NBT
  strings back as plain UTF-8.
- Since 4.2.2: lean mode, only with `MCC_LEAN` (above). The Linux packages are published ReadyToRun (code compiled
  ahead of time: less to compile at start, and code pages that all clients on a machine share).
- Since 4.2.4: the console line looks (above), only with `MCC_CONSOLE_KINDS`; the item spans of 4.2.3 keep a
  translation's arguments' own looks.
- Other changes the bridge needed, active for every user:
  - the client follows the server's cursor item (1.21.2+ cursor packet, carried item of a full resync);
  - an item's removed components are kept (`Item.RemovedComponents`);
  - when a server sent its own enchantment registry, an id it does not name as a vanilla enchantment is
    reported as unknown instead of the vanilla enchantment that has that id by default;
  - bots get every chat line through `ChatBot.OnChatReceived(text, message)`, with `message.isOverlay`,
    `message.isProfileless` and (while the agent bridge runs) `message.clicks`;
  - `McClient.SendRenameItemInWindow(windowId, name)` names the item in a given anvil window.
