# dotnet-features-changelogs

Библиотека для .NET 10, которая **при сборке** приложения читает XML-комментарии классов настроек и правила [FluentValidation](https://docs.fluentvalidation.net/) **из исходников через Roslyn**, затем пишет:

- `docs/settings.md` — каталог настроек и журнал изменений на русском языке относительно предыдущего снимка
- `docs/settings.snapshot.json` — канонический JSON-снимок для следующего сравнения

Демонстрационное приложение: [`samples/DemoApp`](samples/DemoApp).

## Как это работает

```text
dotnet build
    → компиляция + XML-документация
    → FluentDocs.Tool (post-build)
        → CSharpCompilation из @(Compile) + @(ReferencePath)
        → находит AbstractValidator<T> / IValidator<T> в синтаксических деревьях
        → обходит RuleFor / RuleForEach / When / SetValidator / ChildRules
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
  --sources-list obj/fluentdocs.sources.txt \
  --references-list obj/fluentdocs.refs.txt \
  --xml samples/DemoApp/bin/Debug/net10.0/DemoApp.xml \
  --output samples/DemoApp/docs/settings.md \
  --snapshot samples/DemoApp/docs/settings.snapshot.json
```

После `dotnet build` таргет сам пишет списки исходников и ссылок в `obj/`.

## Что попадает в каталог

- Типы с `[SettingsDocs]`. Если атрибута нет — типы, для которых есть `IValidator<T>` и имя оканчивается на `Options` / `Settings` / `Configuration` / `Config`.
- Свойства, XML `<summary>` / `<remarks>`, значения по умолчанию из инициализаторов.
- Правила FluentValidation с нормализованным id (`NotEmpty`, `MaximumLength:50`, `InclusiveBetween:1-3600`, `Matches:^...$`, `Must`, `PrecisionScale:4,2`, `IsEnumName:Status`, `Custom`, …).
- Вложенность: `SetValidator`, `RuleForEach` / `ForEach` / `ChildRules` / `SetInheritanceValidator` → dotted path (`Retry.MaxAttempts`, `Recipients[].Email`, `Contact.Phone`).
- Условные правила (`When` / `Unless`) помечаются как условные.
- Именованные `IPropertyValidator` попадают как `Validator:TypeName`.
- Правила из вспомогательных методов того же валидатора и из конструкторов **с параметрами** (DI): экземпляр валидатора не создаётся.

Журнал изменений сравнивает **идентификаторы правил**, а не сырой текст: изменение `MaximumLength:50` → `MaximumLength:255` видно как удаление + добавление. Неизменившийся `NotEmpty` в журнал не попадает.

## Ограничения v1

- Разбирается fluent-API в исходниках текущей компиляции. Валидаторы из других сборок без исходников не раскрываются.
- `Must` включает текст предиката, если это выражение; иначе — общее «пользовательское условие». `Custom` фиксируется без тела лямбды.
- Условие `When`/`Unless` фиксируется как флаг, без сериализации лямбды.
- `Transform`, `CascadeMode`, `Severity`, `WithName` в каталог не попадают.

## Сборка и тесты

Требуется .NET SDK 10.

```bash
dotnet test
dotnet build samples/DemoApp
```

После сборки демо смотрите [`samples/DemoApp/docs/settings.md`](samples/DemoApp/docs/settings.md).
