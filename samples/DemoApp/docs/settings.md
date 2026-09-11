# Application settings

Generated from FluentValidation validators and XML documentation comments.

## Changelog

No settings changes since the previous snapshot.

## Catalog

### MailOptions (`Mail`)

SMTP mail delivery settings.

Bound to the Mail configuration section.

**Type:** `DemoApp.Options.MailOptions`

#### AllowedSenderPatterns

Regular expressions that outgoing sender addresses must match.

- **Type:** `List<string>`
- **Default:** `[]`
- **Constraints:** none discovered

#### AllowedSenderPatterns[]

Regular expressions that outgoing sender addresses must match.

- **Type:** `string[]`
- **Default:** `[]`
- **Constraints:**
  - Must match pattern `^.*$`. (`Matches:^.*$`)
  - Must not be empty. (`NotEmpty`)

#### EnableTls

When true, the client starts a TLS session after connect.

- **Type:** `bool`
- **Default:** `true`
- **Constraints:** none discovered

#### From

Envelope sender address used for outgoing mail.

- **Type:** `string`
- **Default:** `"noreply@localhost"`
- **Constraints:**
  - Must be a valid email address. (`EmailAddress`)
    - Message: From must be a valid email address.
  - Must not be empty. (`NotEmpty`)

#### Host

SMTP host name or address.

- **Type:** `string`
- **Default:** `"localhost"`
- **Constraints:**
  - Maximum length is 255. (`MaximumLength:255`)
    - Message: SMTP host is required and must be at most 255 characters.
  - Must not be empty. (`NotEmpty`)

#### Port

SMTP TCP port.

- **Type:** `int`
- **Default:** `25`
- **Constraints:**
  - Must be greater than or equal to 465. _(conditional)_ (`GreaterThanOrEqual:465`)
    - Message: TLS mail typically uses port 465 or 587.
  - Must be between 1 and 65535 (inclusive). (`InclusiveBetween:1-65535`)

#### Recipients

Recipients that always receive a copy of system notifications.

- **Type:** `List<RecipientOptions>`
- **Default:** `[]`
- **Constraints:**
  - Must not be null. (`NotNull`)

#### Recipients[]

Recipients that always receive a copy of system notifications.

- **Type:** `RecipientOptions[]`
- **Default:** `[]`
- **Constraints:** none discovered

#### Recipients[].Email

Recipient email address.

- **Type:** `string`
- **Constraints:**
  - Must be a valid email address. (`EmailAddress`)
  - Must not be empty. (`NotEmpty`)

#### Recipients[].Name

Optional display name.

- **Type:** `string`
- **Constraints:**
  - Maximum length is 100. _(conditional)_ (`MaximumLength:100`)

#### Retry

Retry policy applied after a failed send.

- **Type:** `RetryOptions`
- **Constraints:** none discovered

#### Retry.DelayMilliseconds

Delay between attempts in milliseconds.

- **Type:** `int`
- **Default:** `200`
- **Constraints:**
  - Must be greater than 0. (`GreaterThan:0`)
  - Must be less than or equal to 60000. (`LessThanOrEqual:60000`)

#### Retry.MaxAttempts

Maximum number of send attempts, including the first try.

- **Type:** `int`
- **Default:** `3`
- **Constraints:**
  - Must be between 1 and 10 (inclusive). (`InclusiveBetween:1-10`)

#### TimeoutSeconds

Send timeout in seconds.

- **Type:** `int`
- **Default:** `30`
- **Constraints:**
  - Must be between 1 and 300 (inclusive). (`InclusiveBetween:1-300`)

### StorageOptions (`Storage`)

File storage settings for demo artifacts.

**Type:** `DemoApp.Options.StorageOptions`

#### BucketName

Object-storage bucket name. Required when Provider is S3.

- **Type:** `string`
- **Default:** `null`
- **Constraints:**
  - Must match pattern `^[a-z0-9.-]{3,63}$`. _(conditional)_ (`Matches:^[a-z0-9.-]{3,63}$`)
    - Message: BucketName must be a valid S3 bucket name.
  - Must not be empty. _(conditional)_ (`NotEmpty`)

#### MaxFileSizeBytes

Maximum uploaded file size in bytes.

- **Type:** `long`
- **Default:** `1048576`
- **Constraints:**
  - Must be between 1 and 104857600 (inclusive). (`InclusiveBetween:1-104857600`)

#### Provider

Storage backend identifier. Supported values: Local, S3.

- **Type:** `string`
- **Default:** `"Local"`
- **Constraints:**
  - Must satisfy a custom predicate. (`Must`)
    - Message: Provider must be Local or S3.
  - Must not be empty. (`NotEmpty`)

#### RootPath

Absolute directory used when Provider is Local.

- **Type:** `string`
- **Default:** `"/var/demo/data"`
- **Constraints:**
  - Must satisfy a custom predicate. (`Must`)
    - Message: RootPath must be an absolute Unix path.
  - Must not be empty. (`NotEmpty`)

