# ImportDialogApp

Минимальное WPF-приложение с диалогом импорта LIS.

## Реализовано

- Отдельная ветка с полностью очищенным старым содержимым и новым проектом.
- `ImportDialog` на базе вашего шаблона:
  - левая колонка — доступные кривые;
  - правая колонка — выбранные кривые;
  - кнопки `>`, `<`, `>>`, `<<` для переноса.
- `ImportDialogViewModel`:
  - принимает в конструкторе `IEnumerable<CurveItem>` и `IEnumerable<ParameterTable>`;
  - поддерживает несколько таблиц параметров (переключение через ComboBox);
  - по кнопке **Готово** фиксирует результат.
- Возврат результата:
  - после `ShowDialog()` берется `dialog.SelectedCurvesResult` — это кривые, отобранные в правый столбец.
- Простое окно запуска (`MainWindow`) с кнопкой открытия диалога.

## Основные файлы

- `ImportDialogApp/Dialogs/ImportDialog.xaml`
- `ImportDialogApp/Dialogs/ImportDialog.xaml.cs`
- `ImportDialogApp/ViewModels/ImportDialogViewModel.cs`
- `ImportDialogApp/Models/CurveItem.cs`
- `ImportDialogApp/Models/ParameterTable.cs`
- `ImportDialogApp/Models/ParameterRow.cs`

## Запуск и тест в Cursor Cloud

Добавлена конфигурация окружения для Cursor:

- `.cursor/environment.json`
- `.cursor/install.sh`
- `.cursor/start.sh`
- `scripts/test.sh`

Что это дает:

- на старте окружения ставится .NET SDK (если его нет);
- команда проверки:

```bash
bash scripts/test.sh
```

Скрипт проверяет наличие `dotnet`, выполняет `restore` и `build` проекта (`.NET Framework 4.8`) c reference assemblies:

```bash
dotnet restore ImportDialogApp/ImportDialogApp.csproj -p:EnableWindowsTargeting=true
dotnet build ImportDialogApp/ImportDialogApp.csproj -c Release --no-restore -p:EnableWindowsTargeting=true
```

