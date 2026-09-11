# FluentDocs

Библиотека для .NET 10, которая **при сборке** приложения читает XML-комментарии классов настроек и правила [FluentValidation](https://docs.fluentvalidation.net/), затем пишет:

- `docs/settings.md` — каталог настроек и журнал изменений на русском языке относительно предыдущего снимка
- `docs/settings.snapshot.json` — канонический JSON-снимок для следующего сравнения

Демонстрационное приложение: [`samples/DemoApp`](samples/DemoApp).

## Как это работает

```text
dotnet build
    → компиляция + XML-документация
    → FluentDocs.Tool (post-build)
        → находит IValidator<T> / AbstractValidator<T>
        → CreateDescriptor() + humanize правил
        → склеивает XML-комментарии
        → diff с предыдущим snapshot
        → settings.md + settings.snapshot.json
```

Потребитель подключает `FluentDocs.Build` как development dependency (`PrivateAssets="all"`). В runtime приложения анализатор не попадает. Для секции конфигурации на класс настроек вешается `[SettingsDocs("Mail")]` из пакета `FluentDocs.Abstractions`.

## Подключение

```xml
<ItemGroup>
  <PackageReference Include="FluentDocs.Abstractions" Version="1.0.0" />
  <PackageReference Include="FluentDocs.Build" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

Локально в этом репозитории демо импортирует MSBuild-таргеты напрямую и указывает путь к `FluentDocs.Tool.dll`.

MSBuild-свойства:

| Свойство | По умолчанию | Назначение |
| --- | --- | --- |
| `FluentDocsEnabled` | `true` | Включить генерацию после `Build` |
| `FluentDocsOutputPath` | `$(MSBuildProjectDirectory)\docs\settings.md` | Markdown |
| `FluentDocsSnapshotPath` | `$(MSBuildProjectDirectory)\docs\settings.snapshot.json` | JSON-снимок |
| `FluentDocsFailOnError` | `true` | Падать ли сборке при ошибке генерации |
| `FluentDocsToolDll` | `tools/net10.0/FluentDocs.Tool.dll` в пакете | Путь к CLI |

CLI вручную:

```bash
dotnet exec src/FluentDocs.Tool/bin/Debug/net10.0/FluentDocs.Tool.dll \
  --assembly samples/DemoApp/bin/Debug/net10.0/DemoApp.dll \
  --xml samples/DemoApp/bin/Debug/net10.0/DemoApp.xml \
  --output samples/DemoApp/docs/settings.md \
  --snapshot samples/DemoApp/docs/settings.snapshot.json
```

## Что попадает в каталог

- Типы с `[SettingsDocs]`. Если атрибута нет — типы, для которых есть `IValidator<T>` и имя оканчивается на `Options` / `Settings` / `Configuration` / `Config`.
- Свойства, XML `<summary>` / `<remarks>`, значения по умолчанию (parameterless constructor).
- Правила FluentValidation с нормализованным id (`NotEmpty`, `MaximumLength:50`, `InclusiveBetween:1-3600`, `Matches:^...$`, `Must`, …).
- Вложенность: `SetValidator`, `RuleForEach` / `ChildRules` → dotted path (`Retry.MaxAttempts`, `Recipients[].Email`).
- Условные правила (`When` / `Unless`) помечаются как условные.

Журнал изменений сравнивает **идентификаторы правил**, а не сырой текст: изменение `MaximumLength:50` → `MaximumLength:255` видно как удаление + добавление. Неизменившийся `NotEmpty` в журнал не попадает.

## Ограничения v1

- Валидатор должен иметь **public parameterless constructor**. Валидаторы с DI пока пропускаются (предупреждение в markdown).
- Исходники **не** разбираются Roslyn’ом: анализ идёт по уже собранной сборке и `IValidator.CreateDescriptor()`.
- Кастомный `Must` документируется как пользовательское условие; осмысленный текст берётся из `WithMessage`.
- Условие `When`/`Unless` фиксируется как флаг, без сериализации лямбды.

## Сборка и тесты

Требуется .NET SDK 10.

```bash
dotnet test
dotnet build samples/DemoApp
```

После сборки демо смотрите [`samples/DemoApp/docs/settings.md`](samples/DemoApp/docs/settings.md).
