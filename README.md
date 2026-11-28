********************************************************************************************************************
RUS/ru (Russian Language)
********************************************************************************************************************
BLAZE MACRO 1.5 — Windows x64

ЭТО НЕ ОФИЦИАЛЬНОЕ, НЕ КОММЕРЧЕСКОЕ приложение для механических клавиатур, включая DEXP Blaze.
Это программный макрорекордер, а не официальный драйвер от компании DEXP.

Программа разрабатывалась под клавиатуру Dexp Blaze, но так же поддерживает и другие.

ЗАПУСК
Откройте app\BlazeMacro.exe. Установка .NET отдельно не требуется.
Приложение должно оставаться открытым; его можно свернуть.
Активен один открытый макрос. Другие макросы можно хранить в отдельных файлах.

БЫСТРЫЙ СТАРТ
1. Нажмите «Записать». Если шаги уже есть, подтвердите их замену.
2. За 3 секунды переключитесь в нужное приложение.
3. Выполните последовательность клавиш и кнопок мыши. Нажмите F9, чтобы закончить запись.
4. Вернитесь в Blaze Macro, проверьте шаги и сохраните макрос.
5. Переключитесь в целевое окно. Нажмите и отпустите F6.
6. F9 немедленно запрашивает остановку воспроизведения.

Запись сохраняет нажатия, отпускания и интервалы между ними.
Начальное ожидание до первого нажатия не записывается.
Удерживаемые на момент завершения записи клавиши получают шаг отпускания.
Новая запись заменяет предыдущую последовательность.
Запись идёт только после явного нажатия «Записать» и обратного отсчёта.
Она находится в памяти до ручного сохранения. Макросы никуда не отправляются.
Основная панель подсветки обращается прямо к клавиатуре через Windows HID API.

ГРАФИЧЕСКИЙ РЕДАКТОР
• Выберите тип действия, клавишу и длительность, затем нажмите «+ Шаг».
• Щелчок по экранной клавиатуре добавляет шаг выбранного типа.
• Новый шаг вставляется после выделенного, либо в конец пустого списка.
• Выделите существующий шаг, измените поля и нажмите «Применить к шагу».
• Кнопки «Выше» и «Ниже» меняют порядок. Есть дублирование и удаление.
• Для «Нажать и отпустить» длительность означает время удержания.
• Для «Пауза» длительность означает ожидание без нового ввода.
• «Зажать» и «Отпустить» не добавляют задержку. Между ними можно вставить паузу.
• Не показанные на экранной клавиатуре клавиши доступны в выпадающем списке.

МЫШЬ
В списке клавиш и отдельном ряду редактора доступны ЛКМ, ПКМ, средняя кнопка,
X1 и X2. Для них работают «Нажать и отпустить», «Зажать» и «Отпустить».
При записи кнопки мыши и клавиатура попадают в одну последовательность с паузами.
Клики по собственным окнам Blaze Macro не добавляются в запись.
Позиция курсора и перемещения НЕ записываются: каждый клик происходит в текущем
положении указателя. Наведите курсор на нужный элемент перед запуском макроса.
Прокрутка колеса не поддерживается; нажатие колеса — это средняя кнопка мыши.
Для двойного клика добавьте два нажатия ЛКМ с небольшой паузой между ними.
F9 отпускает также кнопки мыши, удерживаемые самим макросом.
Старые файлы открываются. Макросы с мышью сохраняются в формате версии 2;
Blaze Macro 1.0 их не откроет.

ПОДСВЕТКА
Кнопка «Подсветка…» открывает собственный модуль без OpenRGB и RGB-библиотек.
Поддерживаемая ревизия: USB 320F:5084, bcdDevice 0101, профиль 0,
HID-интерфейс MI_01/COL04, Usage FF1C:0092, сигнатура возможностей AA 55.
Модуль использует данные о формате протокола из публичных исследований;
готовый код RGB-драйверов не включён в программу.

1. Нажмите «Прочитать с клавиатуры».
2. Выберите эффект, яркость 0–4, скорость 0–5 (0 — самая быстрая) и цвет.
3. Для фиксированного цвета отключите «Автоматические цвета эффекта».
4. Нажмите «Применить». Перед записью программа перечитывает состояние,
   проверяет, что оно не изменилось, и сохраняет резервную копию.
5. «Вернуть предыдущие» восстанавливает предыдущий заголовок подсветки
   в рамках текущего окна. После закрытия окна эта история сбрасывается.

Запись пока экспериментальная: на реальной клавиатуре проверено чтение
и сигнатура протокола, но программное изменение света не выполнялось.
Проверены формирование пакетов, границы записи и разбор ответов.
Наличие корректного ответа после записи не заменяет визуальную проверку.

Команды записи могут сохранять настройки во flash-памяти клавиатуры.
Изменения отправляются только по кнопке, одинаковые данные повторно не пишутся.
Запись ограничена 8 байтами заголовка подсветки по смещению 1; таблицы клавиш,
прошивка, частота опроса и индивидуальные LED-таблицы не затрагиваются.
При ошибке нет автоматического повторения записи. Прочитайте состояние заново.
Резервные копии: %LOCALAPPDATA%\BlazeMacro\LightingBackups.
В файле хранится прежнее состояние и запрошенный заголовок; импорт из файла
пока не реализован. Путь к копии показывается после применения.
Имена режимов соответствуют исследованному протоколу EVision; все эффекты
конкретной ревизии ещё не проверены. Режим Custom и другие профили не поддержаны.

«Диагностика HID…» читает только метаданные USB 320F:5084 и 04D9:A09F,
без записи нажатий и передачи конфигурационных отчётов. Её результат
сохраняется в TXT/JSON. Чтение подсветки, в отличие от этой диагностики,
отправляет запросы чтения 03/05 и принимает ответ, но не меняет параметры.
Командная строка: app\BlazeMacro.exe --diagnose-hid "папка для отчёта"
Проверка чтения подсветки: app\BlazeMacro.exe --check-direct-reading "папка для отчёта"

Аппаратные сочетания из инструкции DEXP Blaze:
  Fn + Scroll Lock — сменить эффект (19 режимов);
  Fn + Home — сменить цвет;
  Fn + ↑ / ↓ — изменить яркость;
  Fn + ← / → — изменить скорость эффекта.
Эти сочетания нужно нажимать физически на клавиатуре.

Источник сочетаний:
https://drv.dns-shop.ru/drivers/oth/D/dexp-blaze_instrukcia_170555_11122023.pdf
Источники формата протокола и идентификации:
https://gitlab.com/CalcProgrammer1/OpenRGB/-/merge_requests/2487.diff
https://github.com/hex-dump/perihub/blob/main/docs/protocols/evision.md
https://github.com/CalcProgrammer1/OpenRGB/tree/master/Controllers/EVisionKeyboardController
Старый необязательный адаптер OpenRGB сохранён в исходниках для совместимости
разработки, но основное окно его не открывает и для подсветки он не требуется.

Пример Ctrl+C:
  Зажать Left Ctrl → Нажать и отпустить C (50 мс) → Отпустить Left Ctrl.
Перед запуском программа проверяет, что все зажатые клавиши отпущены.

КЛАВИША ЗАПУСКА
Нажмите «F6 · изменить» и физически нажмите другую клавишу.
Esc отменяет назначение. F9 зарезервирована для остановки.
Отдельные Ctrl, Shift, Alt и Windows не назначаются как клавиша запуска.
Запуск происходит при отпускании выбранной клавиши, чтобы она не мешала макросу.
Клавиша запуска поглощается вне окна редактора, когда горячая клавиша включена.
Комбинации с другими удерживаемыми клавишами не запускают макрос.
Для обычного использования этой клавиши отключите флажок горячей клавиши.
Горячая клавиша не запускает макрос при редактировании внутри Blaze Macro.

ТОЧНОСТЬ ПАУЗ
Нажмите «Паузы: Обычный…» рядом с кнопками воспроизведения.
Доступны три режима:
• Обычный — Task.Delay, как в предыдущих версиях.
• Запрос 1 мс — обычный Windows waitable timer + timeBeginPeriod(1).
• Точный — высокоточный Windows waitable timer + timeBeginPeriod(1).
Если Windows не поддерживает высокоточный таймер, используется обычный
waitable timer; фактический механизм указан в JSON-отчёте замера.
Выберите режим и нажмите «Использовать для макроса», затем сохраните макрос.
Старые макросы остаются в обычном режиме. Новый режим сохраняется в файле
версии 3, который предыдущие версии программы не откроют.

Режим влияет на паузы, удержание при «Нажать и отпустить» и интервалы повторов.
Параметр 1 мс — запрос точности, а не обещание срабатывания ровно через 1 мс.
Все ожидания выполняются вне UI; постоянного активного опроса процессора нет.
F9 прерывает ожидание, в том числе длинное, и отпускает удерживаемые клавиши.
Каждому успешному timeBeginPeriod(1) соответствует timeEndPeriod(1) после
завершения, остановки или ошибки. В простое наш запрос не удерживается.
Приоритет процесса, USB, реестр, загрузочные параметры и драйверы не меняются.
На старых Windows до Windows 10 2004 timeBeginPeriod имел системное действие;
на современных Windows настройка не обещает улучшения других процессов.
Высокая точность может увеличить энергопотребление. В Windows 11 видимость
окна (включая свёрнутое состояние) может влиять на точность таймеров.

«Сравнить режимы» проверяет паузы 1, 5, 10 и 20 мс, 60 раз каждую, после
пяти прогревочных ожиданий. Замер не нажимает клавиши и не двигает мышь.
Показаны среднее, P95, максимум и средняя лишняя задержка в миллисекундах.
P95 — значение, не превышенное в 95% замеров. Режимы тестируются последовательно,
поэтому нагрузка может влиять на сравнение. Это не замер задержки клавиатуры,
USB, сети, монитора или игры. Снижение задержки в игре из него не следует.
Отчёт сохраняется в TXT/JSON. Тест можно остановить без сохранения частичных данных.
Для замера без UI: app\BlazeMacro.exe --benchmark-timing "папка для отчёта"


ПРОВЕРКА И ПОВТОРЫ
«Проверить через 3 с» даёт время переключиться в целевое окно.
Воспроизведение направляется в текущее активное окно Windows.
Не переключайте окно во время воспроизведения, если не хотите сменить получателя.
Доступно 1–1000 повторов и задержка между ними. F9 останавливает весь цикл.
При отмене программа отправляет отпускание клавиш, которые сама удерживала.
Если закрыть окно во время операции, сначала операция будет остановлена;
для выхода закройте окно ещё раз.

СОХРАНЕНИЕ
Формат: *.blaze.json. Файл содержит название, клавишу запуска, повторы и шаги.
Перед закрытием, созданием или открытием другого макроса приложение предлагает
сохранить несохранённые изменения. Автозапуска и автоматической загрузки нет.

СОВМЕСТИМОСТЬ И ГРАНИЦЫ
• Работает на уровне Windows, а не во встроенной памяти клавиатуры.
• Не различает несколько подключённых клавиатур: запуск возможен с любой.
• Fn и некоторые аппаратные мультимедийные комбинации могут не передаваться Windows.
• Записывает клавиатуру и пять кнопок мыши, но не перемещения и прокрутку.
• Воспроизводит клавиши, а не готовый текст: раскладка и Caps Lock влияют на символы.
• Приложения с повышенными правами могут не принимать ввод от обычного процесса.
  Если целевое приложение запущено от администратора, может потребоваться запуск
  Blaze Macro с теми же правами. По умолчанию повышение прав не запрашивается.
• Некоторые игры и защищённые приложения не принимают синтетический ввод.
• Это не система жёсткого реального времени: интервалы зависят от нагрузки Windows.
• На самой DEXP Blaze аппаратная проверка не выполнялась.

ИСХОДНИКИ И ПРОВЕРКИ
Исходники C# / WinForms находятся в папке src. Для сборки нужен .NET SDK 10.
Сборка: dotnet build src\BlazeMacro.csproj -c Release
Переносимая сборка:
dotnet publish src\BlazeMacro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o app


ИГРЫ
Автоматизация игровых действий может нарушать правила конкретной игры.
Гарантий отсутствия блокировки нет. Автор не несёт ответственности, если при использовании данной программы,
вы получили блокировку своей учётной записи.

Если вы скачали данную утилиту с вирусами, рекламными приложениями или заплатили деньги, то вас обманули, и вы наткнулись на копию или подделку!


Документация Windows API:
https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc
https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct

<img width="1145" height="863" alt="main" src="https://github.com/user-attachments/assets/a1d9b6d7-7c0a-4646-b548-bb4b4311115f" />

<img width="1145" height="865" alt="work" src="https://github.com/user-attachments/assets/8913dab4-3925-4c6e-b893-81a2bdf28f10" />

<img width="1145" height="865" alt="submenu2" src="https://github.com/user-attachments/assets/f0042399-729b-4454-b722-d4e72f281ed4" />





Мои контакты:
TG: @uranium_23803
GitHub : https://github.com/uranium23803

********************************************************************************************************************
ENG/en (English Language)
********************************************************************************************************************

BLAZE MACRO 1.5 — Windows x64

THIS IS AN UNOFFICIAL, NON-COMMERCIAL application for mechanical keyboards, including the DEXP Blaze.
It is a software macro recorder, not an official driver from DEXP.

GETTING STARTED
Open app\BlazeMacro.exe. No separate .NET installation is required.
The application must remain running; you can minimize it.
Only one open macro is active at a time. Other macros can be stored in separate files.

QUICK START
1. Click “Record”. If there are existing steps, confirm that you want to replace them.
2. Switch to the target application within 3 seconds.
3. Perform the sequence of keyboard and mouse button actions. Press F9 to stop recording.
4. Return to Blaze Macro, review the steps, and save the macro.
5. Switch to the target window. Press and release F6.
6. F9 immediately requests that playback stop.

Recording captures presses, releases, and the intervals between them.
The initial delay before the first press is not recorded.
Release steps are added for any keys still held when recording ends.
A new recording replaces the previous sequence.
Recording starts only after you explicitly click “Record” and the countdown ends.
It remains in memory until you save it manually. Macros are not sent anywhere.
The main lighting panel communicates directly with the keyboard through the Windows HID API.

VISUAL EDITOR
• Select an action type, key, and duration, then click “+ Step”.
• Clicking the on-screen keyboard adds a step of the selected type.
• A new step is inserted after the selected step, or into the list if it is empty.
• Select an existing step, edit its fields, and click “Apply to Step”.
• The “Move Up” and “Move Down” buttons change the order. Steps can also be duplicated or deleted.
• For “Press and Release”, duration specifies how long the key is held.
• For “Pause”, duration specifies a wait without generating new input.
• “Hold Down” and “Release” do not add a delay. You can insert a pause between them.
• Keys not shown on the on-screen keyboard are available in the drop-down list.

MOUSE
The key list and a separate editor row include the left, right, and middle mouse buttons,
as well as X1 and X2. They support “Press and Release”, “Hold Down”, and “Release”.
During recording, mouse button and keyboard actions are combined into a single sequence with pauses.
Clicks on Blaze Macro’s own windows are not added to the recording.
Cursor position and movement are NOT recorded: each click occurs at the pointer’s
current position. Move the pointer over the desired element before starting the macro.
Mouse wheel scrolling is not supported; pressing the wheel is the middle mouse button.
For a double-click, add two left-button clicks with a short pause between them.
F9 also releases any mouse buttons held by the macro itself.
Older files can still be opened. Macros containing mouse actions are saved in version 2
of the file format; Blaze Macro 1.0 cannot open them.

LIGHTING
The “Lighting…” button opens the built-in module, which does not use OpenRGB or RGB libraries.
Supported revision: USB 320F:5084, bcdDevice 0101, profile 0,
HID interface MI_01/COL04, Usage FF1C:0092, capability signature AA 55.
The module uses protocol format information from publicly available research;
no ready-made RGB driver code is included in the application.

1. Click “Read from Keyboard”.
2. Select an effect, brightness from 0–4, speed from 0–5 (0 is the fastest), and a color.
3. For a fixed color, disable “Automatic Effect Colors”.
4. Click “Apply”. Before writing, the application reads the state again,
   checks that it has not changed, and saves a backup.
5. “Restore Previous Settings” restores the previous lighting header
   within the current window session. This history is cleared when the window closes.

Writing is still experimental: reading and the protocol signature have been verified
on a physical keyboard, but changing the lighting through software has not been tested.
Packet construction, write boundaries, and response parsing have been tested.
A valid response after writing does not replace a visual check.

Write commands may save settings to the keyboard’s flash memory.
Changes are sent only when you click the button; identical data is not written again.
Writing is restricted to the 8-byte lighting header at offset 1; key tables,
firmware, polling rate, and individual LED tables are not modified.
Failed writes are not retried automatically. Read the state again.
Backups: %LOCALAPPDATA%\BlazeMacro\LightingBackups.
Each file stores the previous state and the requested header; importing from a backup
file has not yet been implemented. The backup path is displayed after applying changes.
Mode names follow the researched EVision protocol; not all effects have been verified
on this particular revision. Custom mode and other profiles are not supported.

“HID Diagnostics…” reads only metadata for USB devices 320F:5084 and 04D9:A09F,
without recording keystrokes or sending configuration reports. Its results
can be saved as TXT/JSON. Unlike these diagnostics, reading the lighting state
sends read requests 03/05 and receives a response, but does not change any settings.
Command line: app\BlazeMacro.exe --diagnose-hid "report folder"
Lighting read check: app\BlazeMacro.exe --check-direct-reading "report folder"

Hardware shortcuts from the DEXP Blaze manual:
  Fn + Scroll Lock — change the effect (19 modes);
  Fn + Home — change the color;
  Fn + ↑ / ↓ — adjust brightness;
  Fn + ← / → — adjust effect speed.
These shortcuts must be pressed physically on the keyboard.

Shortcut source:
https://drv.dns-shop.ru/drivers/oth/D/dexp-blaze_instrukcia_170555_11122023.pdf
Protocol format and device identification sources:
https://gitlab.com/CalcProgrammer1/OpenRGB/-/merge_requests/2487.diff
https://github.com/hex-dump/perihub/blob/main/docs/protocols/evision.md
https://github.com/CalcProgrammer1/OpenRGB/tree/master/Controllers/EVisionKeyboardController
The old optional OpenRGB adapter remains in the source code for development compatibility,
but it is not opened by the main window and is not required for lighting control.

Ctrl+C example:
  Hold Down Left Ctrl → Press and Release C (50 ms) → Release Left Ctrl.
Before playback, the application checks that all held keys have been released.

TRIGGER KEY
Click “F6 · Change” and physically press another key.
Esc cancels the assignment. F9 is reserved for stopping.
Ctrl, Shift, Alt, and Windows keys cannot be assigned individually as the trigger key.
Playback starts when the selected key is released so that it does not interfere with the macro.
Outside the editor window, the trigger key is intercepted when the hotkey is enabled.
Combinations with other held keys do not start the macro.
To use this key normally, clear the hotkey checkbox.
The hotkey does not start the macro while you are editing inside Blaze Macro.

PAUSE ACCURACY
Click “Pauses: Standard…” next to the playback controls.
Three modes are available:
• Standard — Task.Delay, as in previous versions.
• 1 ms Request — a standard Windows waitable timer + timeBeginPeriod(1).
• Precise — a high-resolution Windows waitable timer + timeBeginPeriod(1).
If Windows does not support the high-resolution timer, a standard waitable timer
is used instead; the actual mechanism is listed in the benchmark’s JSON report.
Select a mode and click “Use for Macro”, then save the macro.
Older macros remain in Standard mode. The new mode is saved in version 3
of the file format, which earlier application versions cannot open.

The mode affects pauses, hold durations for “Press and Release”, and repeat intervals.
The 1 ms setting is a resolution request, not a guarantee of execution exactly 1 ms later.
All waits run outside the UI thread; the application does not continuously busy-wait.
F9 interrupts waits, including long ones, and releases held keys.
Each successful timeBeginPeriod(1) call is paired with timeEndPeriod(1) after
completion, cancellation, or an error. No timer-resolution request is kept active while idle.
Process priority, USB settings, the registry, boot settings, and drivers are not changed.
On older Windows versions before Windows 10 2004, timeBeginPeriod had a system-wide effect;
on modern Windows, this setting does not promise improvements for other processes.
Higher resolution may increase power consumption. On Windows 11, window visibility
—including whether it is minimized—may affect timer accuracy.

“Compare Modes” tests pauses of 1, 5, 10, and 20 ms, with 60 measurements each,
after five warm-up waits. The benchmark does not press keys or move the mouse.
It displays the mean, P95, maximum, and mean excess delay in milliseconds.
P95 is the value not exceeded in 95% of measurements. Modes are tested sequentially,
so system load can affect the comparison. This does not measure keyboard,
USB, network, monitor, or game latency. It does not demonstrate reduced in-game latency.
Reports can be saved as TXT/JSON. The test can be stopped without saving partial results.
To benchmark without the UI: app\BlazeMacro.exe --benchmark-timing "report folder"


TESTING AND REPEATS
“Test in 3 Seconds” gives you time to switch to the target window.
Playback is directed to the currently active Windows window.
Do not switch windows during playback unless you want to change the input recipient.
You can set 1–1000 repeats and a delay between them. F9 stops the entire loop.
When cancelled, the application sends release events for the keys it was holding.
If you close the window during an operation, the operation is stopped first;
close the window again to exit.

SAVING
Format: *.blaze.json. The file contains the name, trigger key, repeats, and steps.
Before closing, creating a new macro, or opening another macro, the application
offers to save unsaved changes. There is no automatic startup or automatic loading.

COMPATIBILITY AND LIMITATIONS
• Operates at the Windows level, not in the keyboard’s onboard memory.
• Does not distinguish between multiple connected keyboards: any of them can trigger playback.
• Fn and some hardware multimedia combinations may not be passed to Windows.
• Records the keyboard and five mouse buttons, but not pointer movement or scrolling.
• Replays keys rather than text: the keyboard layout and Caps Lock affect the characters produced.
• Applications running with elevated privileges may not accept input from a normal process.
  If the target application is running as administrator, you may need to run
  Blaze Macro with the same privileges. Elevation is not requested by default.
• Some games and protected applications do not accept synthetic input.
• This is not a hard real-time system: timing depends on Windows system load.
• Hardware testing on the DEXP Blaze itself has not been performed.

SOURCE CODE AND TESTING
The C# / WinForms source code is in the src folder. Building requires .NET SDK 10.
Build: dotnet build src\BlazeMacro.csproj -c Release
Portable build:
dotnet publish src\BlazeMacro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o app


GAMES
Automating gameplay actions may violate the rules of a particular game.
There is no guarantee that you will not be banned. The author accepts no responsibility
if your account is banned as a result of using this application.

If you downloaded this utility bundled with viruses or adware, or paid money for it,
you have been misled and have encountered a copy or counterfeit version!


Windows API documentation:
https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc
https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct

<img width="1145" height="863" alt="main" src="https://github.com/user-attachments/assets/a1d9b6d7-7c0a-4646-b548-bb4b4311115f" />

<img width="1145" height="865" alt="work" src="https://github.com/user-attachments/assets/8913dab4-3925-4c6e-b893-81a2bdf28f10" />

<img width="1145" height="865" alt="submenu2" src="https://github.com/user-attachments/assets/f0042399-729b-4454-b722-d4e72f281ed4" />

My contacts:
TG: @uranium_23803
GitHub : https://github.com/uranium23803


*****************************************************************************************************************
   FAQ
*****************************************************************************************************************
RUS/ru
******************************************************************************************************************
FAQ — Частые вопросы о BlazeMacro
Что это вообще такое?
BlazeMacro — бесплатная программа с открытым исходным кодом для механических клавиатур, в первую очередь для DEXP Blaze. Она делает две вещи: записывает и воспроизводит макросы (клавиатура + кнопки мыши) и управляет подсветкой напрямую через HID, без OpenRGB.

Проект появился просто: я купил DEXP Blaze, обнаружил, что фирменного софта под неё нет вообще — ни макросов, ни настроек подсветки, — и решил исправить ситуацию. Заодно это поможет всем, кто столкнулся с той же проблемой: таких людей, судя по форумам, немало.

Это не официальный и не коммерческий продукт. Я не связан с DEXP или DNS.

Это вирус? Стиллер? Майнер?
Нет. И вот почему этому можно доверять, а не просто верить на слово:

Весь код открыт — лежит в папке src, его может проверить любой. Там 13 файлов на C#, ничего больше.
Нет сетевой активности. Программа никуда ничего не отправляет. Макросы хранятся локально в файлах *.blaze.json.
Нет рекламы, майнинга, телеметрии.
Можно собрать самому. Если не доверяешь чужим exe — собери свою копию:
text
dotnet build src\BlazeMacro.csproj -c Release
Собранная тобой версия гарантированно соответствует исходникам — это лучший способ убедиться, что внутри нет ничего лишнего.

Если ты скачал эту утилиту с вирусами, рекламой или заплатил за неё деньги — тебя обманули. Ты наткнулся на копию или подделку. Оригинал бесплатен и распространяется только здесь, на GitHub.


Я сломаю клавиатуру кнопкой «Применить» в подсветке?
Это самый частый страх, поэтому объясню подробно.

Запись ограничена 8 байтами заголовка подсветки по смещению 1. Что это значит на практике:

прошивка клавиатуры не затрагивается;
таблицы клавиш не затрагиваются;
частота опроса не затрагивается;
индивидуальные LED-таблицы не затрагиваются.
То есть максимально «глубокий» эффект от записи — подсветка станет выглядеть не так, как было. И даже от этого есть страховка: перед каждой записью программа сохраняет резервную копию текущего состояния в:

%LOCALAPPDATA%\BlazeMacro\LightingBackups
В файле лежит прежнее состояние и запрошенный заголовок. При ошибке записи автоматического повтора нет — просто прочитайте состояние заново.

Почему нет цифровой подписи?
Потому что это бесплатный проект одного человека. Сертификат кодовой подписи стоит денег и требует продления — для хобби-проекта без донатов это просто бессмысленная трата. Отсутствие подписи — не признак вируса, а нормальная ситуация для независимых утилит. Хотите гарантий — собирайте из исходников, они открыты.

Почему программа весит 110 МБ?
Это self-contained сборка .NET: в один exe упакован весь рантайм. Поэтому .NET отдельно устанавливать не нужно — скачал и запустил. Размер не секретный и не подозрительный, так устроены все однофайловые self-contained приложения.

Это работает в играх?
Технически — работает так же, как любой софт, эмулирующий ввод через SendInput. Но честно:

Автоматизация игровых действий может нарушать правила конкретной игры.
Гарантий отсутствия блокировки нет.
Античиты умеют детектировать синтетический ввод, а некоторые игры его вообще не принимают.
Автор не несёт ответственности, если при использовании программы вы получили блокировку учётной записи.
Решение использовать макросы в онлайн-играх — ваше и только ваше. Я вас предупредил, README предупреждает, дальше сами.

Чем это отличается от «официального драйвера»?
Ничем не отличается по названию и всем отличается по сути: это не драйвер. Программа не прошивает клавиатуру и не добавляет ей новых аппаратных функций. Она работает на уровне Windows: макросы воспроизводятся системой, а подсветка управляется через вендорский HID-канал — тем же протоколом EVision, который уже исследован в проекте OpenRGB. Всё, что программа умеет, — уже заложено в железе, она просто даёт к этому доступ.

Как начать пользоваться?
Скачайте релиз или соберите из исходников.
Запустите app\BlazeMacro.exe.
Макрос: нажмите «Записать», за 3 секунды переключитесь в нужное приложение, выполните последовательность, нажмите F9 для завершения. Проверьте шаги, сохраните макрос.
Назначьте клавишу запуска («F6 · изменить» — и физически нажмите нужную клавишу).
В целевом окне нажмите и отпустите клавишу запуска — макрос воспроизведётся. F9 — остановка в любой момент.
Подсветка: кнопка «Подсветка…» → «Прочитать с клавиатуры» → выберите эффект, яркость 0–4, скорость 0–5, цвет → «Применить». До первого чтения запись недоступна — это сделано специально.
Аппаратные сочетания самой клавиатуры (Fn + Scroll Lock, Fn + Home и т. д.) продолжают работать как раньше — программа им не мешает.

Программа ничего не делает. Что проверить?
Целевое приложение запущено от администратора? Тогда запустите BlazeMacro тоже от администратора: обычный процесс не может посылать ввод процессу с повышенными правами. По умолчанию программа права не запрашивает.
Некоторые игры и защищённые приложения блокируют синтетический ввод — это не баг, а их защита.
Конфликт с другими макро-утилитами: если запущены AutoHotkey или подобное, возможны конфликты за ввод.
Проверьте раскладку и Caps Lock: макрос воспроизводит клавиши, а не готовый текст.
Курсор: позиция мыши не записывается. Каждый клик происходит там, где курсор находится в момент запуска. Наведите курсор на нужный элемент заранее.
Почему паузы иногда «плывут»?
Windows — не система жёсткого реального времени: интервалы зависят от нагрузки. В программе есть три режима пауз (Обычный / Запрос 1 мс / Точный) — их можно сравнить встроенным бенчмарком «Сравнить режимы». Он замеряет точность таймеров, но это не замер задержки клавиатуры, USB или игры — снижения задержки в игре из него не следует.

Что точно НЕ умеет программа
Работать без запущенного приложения: макросы живут в памяти ПК, не в клавиатуре.
Различать несколько подключённых клавиатур.
Записывать перемещения мыши и прокрутку колеса.
Переживать закрытие окна: автозапуска и автозагрузки нет.
Передавать Fn и некоторые аппаратные мультимедийные комбинации — их Windows не всегда видит.
Где backups и отчёты
Резервные копии подсветки: %LOCALAPPDATA%\BlazeMacro\LightingBackups
Отчёты диагностики и бенчмарков: сохраняются в TXT/JSON в указанную вами папку. Есть и режим командной строки:
app\BlazeMacro.exe --diagnose-hid "папка" — диагностика HID
app\BlazeMacro.exe --check-direct-reading "папка" — проверка чтения подсветки
Хочу убедиться сам. С чего начать?
Соберите из исходников — лучший способ убедиться, что в бинарнике нет ничего лишнего.
Прогоните exe через VirusTotal. Для утилиты с SendInput и low-level hooks 0–2 срабатывания из 70 — норма (эвристики часто ругаются на эмуляцию ввода). 10+ детектов — повод насторожиться и писать мне.
Откройте код. Начните с Core.cs (логика макросов), EvisionLighting.cs и Lighting.cs (подсветка). Там нет сетевых запросов и ничего нет за пределами папки программы и LightingBackups.

*****************************************************************************************************************
ENG/en
******************************************************************************************************************

FAQ — Frequently Asked Questions about BlazeMacro
What is this?
BlazeMacro is a free, open‑source tool for mechanical keyboards, primarily designed for the DEXP Blaze. It lets you:

Record and replay macros (keyboard + mouse buttons).
Control backlighting directly via HID, without needing OpenRGB.
I built this because I bought a DEXP Blaze and realized there’s no official software at all—no macros, no lighting controls. I wanted to fix that for myself and help others who hit the same wall (and judging by forum threads, there are plenty of them).

This is not an official or commercial product. I’m not affiliated with DEXP or DNS.

Is this a virus? A stealer? A miner?
No. Here’s why you can trust it:

Full source code is public in the src folder—13 C# files, nothing hidden. Anyone can audit it.
No network activity. The app doesn’t send anything anywhere. Macros are saved locally as *.blaze.json files.
No ads, no mining, no telemetry.
You can build it yourself for the strongest guarantee:
bash
dotnet build src\BlazeMacro.csproj -c Release
A build from the published sources matches exactly what’s in the repo—no surprises.

If you paid money for this tool, or downloaded a version with viruses/ads, you’ve been scammed. The original is free and only distributed here on GitHub. Any paid copy is a fake.
Will clicking “Apply” break my keyboard?
This is the biggest worry, so let’s be clear.

The write operation is strictly limited to 8 bytes of the lighting header at offset 1. In practice, that means:

The keyboard’s firmware is untouched.
Key mapping tables are untouched.
Polling rate is untouched.
Individual LED tables are untouched.
At worst, the lighting will look different than before. And there’s a safety net: before every write, the app saves a backup of the current state to:

text
%LOCALAPPDATA%\BlazeMacro\LightingBackups
Each backup includes the previous state and the requested header. If something goes wrong, you can manually restore it. There’s no automatic retry on write failure—just re-read the state and try again.

Full disclosure: the write feature is still experimental. We’ve verified the protocol signature and read states on real hardware, and we’ve tested packet formation, write boundaries, and response parsing in code. But we haven’t done large‑scale live testing on physical units yet. “Experimental” here means “read carefully, use backups,” not “this will brick your device.”

Why doesn’t it have a code signing certificate?
Because this is a one‑person, non‑commercial project. Code signing certificates cost money and need renewal—for a free hobby tool with no donations, it’s not worth it. No signature is normal for independent utilities. If you want maximum confidence, build from source—that’s the gold standard.

Why is the executable 110 MB?
It’s a self‑contained .NET build: the runtime is bundled inside the single EXE. That’s why you don’t need to install .NET separately. The size is expected and not suspicious—this is how all single‑file self‑contained apps work.

Does this work in games? 
Technically, yes: it emulates input using Windows SendInput, just like other macro tools. But be realistic:

Automating gameplay actions can violate a game’s terms of service.
There are no guarantees against account bans.
Anti‑cheat systems can detect synthetic input; some games outright block it.
I’m not responsible if your account gets suspended or banned due to using this tool in online games.
Using macros in competitive online play is entirely your decision. I’ve warned you in the README, I’m warning you here—the rest is on you.

How is this different from an “official driver”?
It isn’t a driver at all. It doesn’t flash the keyboard or add new hardware features. Instead, it works at the Windows level:

Macros run as system input.
Lighting is controlled via the vendor’s HID channel, using the EVision protocol (the same one researched in the OpenRGB project).
Everything it does is already supported by the hardware; BlazeMacro just exposes those capabilities.

Quick Start
Download the release or build from source.
Run app\BlazeMacro.exe.
To record a macro: click “Record”, switch to your target app within 3 seconds, perform your sequence, press F9 to stop. Review the steps and save the macro.
Set a launch key (“F6 · change” then press the key you want).
In the target window, press and release the launch key to run the macro. Press F9 at any time to stop.
For lighting: click “Lighting…”, then “Read from Keyboard”. Choose effect, brightness (0–4), speed (0–5), and color, then click “Apply”. Writing is disabled until you’ve successfully read the state once—this is intentional.
The keyboard’s native shortcuts (Fn + Scroll Lock, Fn + Home, etc.) still work as usual and aren’t affected by the app.

It’s not working. What should I check?
Target app runs as admin? Then run BlazeMacro as admin too. Normal processes can’t inject input into elevated ones. By default, the app doesn’t request admin rights.
Some games and protected apps block synthetic input. This is their security design, not a bug.
Conflicts with other macro tools. If AutoHotkey or similar is running, they may fight over input.
Layout and Caps Lock matter. The macro replays keys, not final characters.
Mouse position isn’t recorded. Every click happens where the cursor is when you start the macro. Position the cursor manually before triggering it.
Why do delays sometimes “drift”?
Windows isn’t a hard real‑time OS: timing depends on system load. The app offers three delay modes (Normal / 1 ms request / Precise) and a built‑in benchmark (“Compare Modes”) to measure timer accuracy. But this measures Windows timers only—it does not measure keyboard latency, USB latency, or game latency, and it won’t reduce in‑game delay.

Things This Tool Definitely Does NOT Do
Run without the app open: macros live in RAM, not in the keyboard.
Distinguish between multiple connected keyboards.
Record mouse movement or wheel scroll.
Auto‑start or auto‑load on boot.
Send Fn or some hardware multimedia combos—Windows doesn’t always expose them.
Where Are Backups and Reports?
Lighting backups: %LOCALAPPDATA%\BlazeMacro\LightingBackups
Diagnostic and benchmark reports: saved as TXT/JSON to a folder you specify. Command‑line options:
app\BlazeMacro.exe --diagnose-hid "folder" — HID diagnostics
app\BlazeMacro.exe --check-direct-reading "folder" — direct read check for lighting
I Want to Verify It Myself. Where Should I Start?
Build from source. This is the best way to be 100% sure there’s nothing extra in the binary.
Scan the EXE with VirusTotal. For a tool that uses SendInput and low‑level hooks, 0–2 detections out of ~70 is typical (heuristics flag input emulation). 10+ detections is a red flag and worth reporting to me.
Read the code. Start with Core.cs (macro logic), EvisionLighting.cs, and Lighting.cs (lighting control). You’ll see no network calls and nothing outside the app folder and LightingBackups.
