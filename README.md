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

