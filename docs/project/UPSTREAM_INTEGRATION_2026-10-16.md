# Інтеграція upstream — 2026-10-16

## Межі оновлення

Обидва незалежні репозиторії інтегрують `aethai/ro3emu-server` до `32b9b9d20aadafc7cb7c400816f52e3efbe6e360` включно. Історія збережена merge-комітом; rebase/force-push не використовувалися. Гілки призначення: Original — `original`, Classic — `main`.

Отримані коміти: `fda6bf2` (battle-pass play/challenges), `f88285a` (setup/export tools), `c24a91d` (Pass I → II/client fixes), `32b9b9d` (guild/event/dungeon/map-module fixes). Upstream повторно перевірений під час інтеграції й залишався на цьому SHA.

## Наші зміни

| Компонент | Результат перевірки |
|---|---|
| `CombatService.Casting.cs` | Збережено незалежну затримку звичайної атаки після скасування, слабкі ключі сесій і перевірку ролі. Прямий регресійний тест перевіряє відмову до завершення затримки й дозвіл після неї. |
| `SkillService.cs` | Збережено атаку поточної професії у вільному PC-слоті, без дублювання та перезапису зайнятого слота. Перевірено новачка, зміну професії, ручне призначення й повторний вхід. |
| `FunctionService.cs` / `GmCommands.cs` | Збережено `!unlockall on/off/status`: лише поточна роль, без обходу структурних передумов функції 1003. Перевірено ізоляцію другої ролі та вимкнення. |
| `.gitignore` | Єдиний merge-конфлікт вирішений об'єднанням локальних виключень із upstream `/server-bin/`. |

Окремо виправлено `ActivityClock.ServerOpen`: цивільна дата сезону конвертується через часовий пояс інжектованого realm clock, не через пояс ОС. На Windows це усуває випадковий перехід відкриття сезону на попередній день у тестах з іншим часовим поясом. Є тест UTC+14; fixture знаходить корінь за `Ro3Emu.slnx`, тому працює й у CI без приватних даних.

Тести upstream адаптовані до наших політик: перевірка PC-слота очікує нормальну атаку; оновлення зайнятого слота приймає коректний protobuf `Inc`, не лише `All`. Багатокрокові reward/quest fixtures чекають завершення атаки, контрольовано зупиняють авто-повтори у fault-injection фазах і не скидають новий cadence-захист. Сценарій Dracula перевіряє послідовність порятунку, не виживання тестового персонажа проти одинадцяти ворогів. Прямі cadence-тести не використовують retry-helper.

Додаткове виправлення `DatabaseBackup`: стартове очищення `.partial` тепер використовує той самий `backupGate`, що й ручна копія. CI виявив `SQLite Error 10` у тесті стартового/manual backup; аналіз коду показав можливість видалення активного `.partial` фоновим очищенням. Це важливо після [зміни запуску BackgroundService у .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task). Протокол SQLite backup, перевірка integrity та retention не змінюються. До фінального Windows-набору додано всі сім наявних `DatabaseBackupTests`.

## Дані та середовище

На ПК є .NET SDK 10.0.402. Експортовані config/nav/sceneeditor/skills і Lua-джерела використовуються локально; відсутня `Localization_en.json` експортована з наявного `re/clientB/luac`. Тестові сервери використовують тимчасові бази й порти, не робочі ігрові БД. `RO3_CLIENT_DIR` задається окремо для Original/Classic, щоб hooks та ClientTools збиралися проти відповідного клієнта.

Повний перший Windows-прогін Original: 1 436 passed, 31 failed, 4 skipped із 1 471. Він виявив несумісні припущення fixtures та відсутні тексти; ці випадки включені до фінального регресійного набору. Паралельні широкі повтори виявляли ресурсно-залежні таймінги; їх зупинено, а виявлені випадки повторно перевірені послідовно з чотирма xUnit threads. **Фінальний повний client-data набір не оголошується зеленим:** приймальні результати нижче стосуються конкретного 87-тестового набору (80 ігрових/клієнтських регресій + 7 backup-тестів) та client-independent CI.

Для повного набору не звужувати портовий пул: `TestPorts` сам ділить його на дві частини й не перевикористовує вже вибрані порти протягом процесу. Штатний діапазон залишає потрібний запас; окремий маленький діапазон для кожного процесу вичерпувався.

## Розгортання та обмеження

- Старий адміністративний deployment, чинні Desktop-лаунчери, клієнтські DLL і робочі SQLite/WAL не змінені. Це оновлення репозиторіїв, не перемикання живого сервера.
- Локальні попередні HEAD збережені у `backup/pre-upstream-20261016`; початковий split backup також залишається.
- Бінарні клієнти, приватні ключі, БД, експорти й логи не комітяться.
- `Pass.Play` / `Pass.FreePremium` залишені як в upstream: Development вмикає їх. Офіційність цих правил не підтверджена; Classic-дизайн не реалізується цим merge.
- Classic ще потребує окремих endpoint/портів/лаунчерів перед ігровим запуском.
- Реальний клієнтський бій, міграція копії робочої БД та приймання в грі цим оновленням не підтверджені.

## Фінальні приймальні результати

| Перевірка | Результат |
|---|---|
| Windows, game/client/backup регресії | **87 passed, 0 failed, 0 skipped** |
| Windows, patcher | **14 passed, 0 failed, 0 skipped** |
| Release: Server, ClientHooks, ClientTools, LuaDecode, ConfigExport | **5/5 успішно**, наявні compiler warnings не приховують помилок збірки |
| Linux CI, client-independent server tests | **495 passed, 0 failed, 870 skipped** (приватних клієнтських даних у CI немає) |
| Linux CI, patcher | **13 passed, 0 failed, 1 skipped** (Windows-only case) |
| Website validator / git diff --check | Успішно |

Перевірений Windows HEAD fixtures: `b17a58e50836a4a116fec4c05575aa0df0a767d9`. [Успішний Linux CI](https://github.com/Patrashurik93/ro3emu-server/actions/runs/38920697358) перевірив серверний код `51e59fac1a04ed5e193c5736af852aca82b2a666`; наступні зміни — лише fixtures та документація, `src/` не змінювався.

Точний фільтр 87 перевірок збережено у [UPSTREAM_INTEGRATION_2026-10-16.filter.txt](UPSTREAM_INTEGRATION_2026-10-16.filter.txt). Повторити на підготовленій локальній копії:

```powershell
$settings = Join-Path $env:TEMP 'ro3-review.runsettings'
'<RunSettings><xUnit><MaxParallelThreads>4</MaxParallelThreads></xUnit></RunSettings>' | Set-Content $settings
$filter = (Get-Content docs/project/UPSTREAM_INTEGRATION_2026-10-16.filter.txt -Raw).Trim()
dotnet test tests/Ro3Emu.Tests -c Release --settings $settings --filter $filter
```

Локальні докази: `TestResults/signoff-final.trx`, `TestResults/patcher-final.trx`. Вони не публікуються разом із приватними даними. Код `src/`, `tests/` і `tools/` звірений між Original/Classic; напрямки поки відрізняються планами, не реалізованими Classic-механіками.
