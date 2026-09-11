# Application settings

Generated from FluentValidation validators and XML documentation comments.

## Changelog

Initial catalog generated.

## Catalog

### SampleMailOptions (`Mail`)

SMTP-like settings used as a generator fixture.

**Type:** `FluentDocs.Tests.Fixtures.SampleMailOptions`

#### EnableTls

Enables TLS after connect.

- **Type:** `bool`
- **Default:** `false`
- **Constraints:** none discovered

#### From

Sender address.

- **Type:** `string`
- **Default:** `"noreply@localhost"`
- **Constraints:**
  - Must be a valid email address. (`EmailAddress`)
  - Must not be empty. (`NotEmpty`)

#### Host

SMTP host name.

- **Type:** `string`
- **Default:** `"localhost"`
- **Constraints:**
  - Maximum length is 255. (`MaximumLength:255`)
    - Message: SMTP host is required and must be at most 255 characters.
  - Must not be empty. (`NotEmpty`)

#### Port

SMTP port.

- **Type:** `int`
- **Default:** `25`
- **Constraints:**
  - Must be greater than or equal to 465. _(conditional)_ (`GreaterThanOrEqual:465`)
  - Must be between 1 and 65535 (inclusive). (`InclusiveBetween:1-65535`)

#### Recipients

Notification recipients.

- **Type:** `List<SampleRecipientOptions>`
- **Default:** `[]`
- **Constraints:** none discovered

#### Recipients[]

Notification recipients.

- **Type:** `SampleRecipientOptions[]`
- **Default:** `[]`
- **Constraints:** none discovered

#### Recipients[].Email

Email address.

- **Type:** `string`
- **Constraints:**
  - Must be a valid email address. (`EmailAddress`)
  - Must not be empty. (`NotEmpty`)

#### Recipients[].Name

Display name.

- **Type:** `string`
- **Constraints:**
  - Maximum length is 100. (`MaximumLength:100`)

#### Retry

Nested retry policy.

- **Type:** `SampleRetryOptions`
- **Constraints:** none discovered

#### Retry.DelayMilliseconds

Delay in milliseconds.

- **Type:** `int`
- **Default:** `200`
- **Constraints:**
  - Must be greater than 0. (`GreaterThan:0`)

#### Retry.MaxAttempts

Maximum attempts.

- **Type:** `int`
- **Default:** `3`
- **Constraints:**
  - Must be between 1 and 10 (inclusive). (`InclusiveBetween:1-10`)

### SampleStorageOptions (`Storage`)

Storage settings fixture.

**Type:** `FluentDocs.Tests.Fixtures.SampleStorageOptions`

#### MaxFileSizeBytes

Maximum file size in bytes.

- **Type:** `int`
- **Default:** `1024`
- **Constraints:**
  - Must be between 1 and 10000000 (inclusive). (`InclusiveBetween:1-10000000`)

#### RootPath

Absolute root path.

- **Type:** `string`
- **Default:** `"/data"`
- **Constraints:**
  - Must match pattern `^/`. (`Matches:^/`)
  - Must not be empty. (`NotEmpty`)

