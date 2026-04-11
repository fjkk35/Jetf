## ADDED Requirements

### Requirement: Login SHALL authenticate the operator account
The system SHALL provide a login screen with an account input box and login action that submits `account` and the installed `versionCode` to `POST /api/auth/login`.

#### Scenario: Successful login
- **WHEN** the user enters an existing account and the API returns `isSuccess = true` with `data = true`
- **THEN** the system SHALL persist the authenticated account in memory and navigate to the main menu screen

#### Scenario: Invalid account
- **WHEN** the user submits the login form and the API returns `isSuccess = true` with `data = false`
- **THEN** the system SHALL remain on the login screen and display a readable login failure message

#### Scenario: Version expired during login
- **WHEN** the login API returns `errorCode = APP_VERSION_EXPIRED`
- **THEN** the system SHALL display the backend message and SHALL not authenticate the user

### Requirement: Menu SHALL provide access to inbound setup
The system SHALL display a main menu with at least one entry for inbound operation and SHALL allow the operator to enter that flow using touch or DT 40 key input.

#### Scenario: Select inbound option
- **WHEN** the operator chooses menu item `1. 入庫`
- **THEN** the system SHALL navigate to the inbound settings screen

### Requirement: Inbound settings SHALL validate required setup data
The system SHALL load source types from `GET /api/shipmentinbound/source-types`, present them in a dropdown list, and require a valid starting sequence number matching two uppercase letters followed by four digits.

#### Scenario: Source types loaded successfully
- **WHEN** the inbound settings screen opens and the API returns one or more source types
- **THEN** the system SHALL populate the dropdown with the returned options and allow the operator to choose one

#### Scenario: Sequence number format is invalid
- **WHEN** the operator enters a starting sequence number that does not match the pattern `[A-Z]{2}[0-9]{4}` and requests the next step
- **THEN** the system SHALL display a validation error and SHALL remain on the inbound settings screen

#### Scenario: Setup confirmed
- **WHEN** the operator has selected a source type and entered a valid starting sequence number, then triggers F4 or the next-step action
- **THEN** the system SHALL store the settings and navigate to the inbound work screen

### Requirement: Inbound work SHALL manage location locking and conditional fields
The system SHALL show the selected source type and current sequence number as read-only labels, SHALL allow the operator to enter a location code, and SHALL lock that location after confirmation until the operator explicitly requests a change.

#### Scenario: Location is locked for operation
- **WHEN** the operator enters a location code and confirms it for the current work session
- **THEN** the system SHALL make the location field read-only before accepting shipment tracking input

#### Scenario: Change location with F4
- **WHEN** the operator is on the inbound work screen and presses F4
- **THEN** the system SHALL unlock the location field and focus it for editing

#### Scenario: Return tracking field is conditionally visible
- **WHEN** the selected source type name is `新竹退件`
- **THEN** the system SHALL display an additional return tracking number input field

#### Scenario: Return tracking field is hidden for other sources
- **WHEN** the selected source type name is not `新竹退件`
- **THEN** the system SHALL hide the return tracking number input field

### Requirement: Tracking submission SHALL check shipment existence before write
The system SHALL accept tracking numbers by scanner or manual input, call `POST /api/shipmentinbound/check` first, and only proceed to `POST /api/shipmentinbound` after the check flow is resolved.

#### Scenario: Shipment check passes
- **WHEN** the operator submits a tracking number and the check API returns `data = true`
- **THEN** the system SHALL immediately call the inbound write API with the current source, sequence number, location code, and optional return tracking number

#### Scenario: Unknown shipment requires operator confirmation
- **WHEN** the operator submits a tracking number and the check API returns `data = false`
- **THEN** the system SHALL display a dialog containing the tracking number and the text `不明貨`, with cancel and confirm actions

#### Scenario: Unknown shipment is cancelled
- **WHEN** the unknown-shipment dialog is shown and the operator chooses cancel
- **THEN** the system SHALL not call the inbound write API and SHALL keep the operator on the inbound work screen

#### Scenario: Unknown shipment is confirmed
- **WHEN** the unknown-shipment dialog is shown and the operator chooses confirm
- **THEN** the system SHALL continue by calling the inbound write API using the current form values

### Requirement: Successful inbound write SHALL advance the sequence number safely
The system SHALL increment the current sequence number by one after a successful inbound write, clear the shipment entry fields for the next scan, and stop the operator when the sequence has reached its upper bound.

#### Scenario: Sequence increments after write
- **WHEN** the inbound write API returns `isSuccess = true` with `data = true` and the current sequence is below `ZZ9999` within the same prefix range
- **THEN** the system SHALL update the displayed sequence number to the next numeric value such as `AB0001` to `AB0002`

#### Scenario: Sequence upper bound reached
- **WHEN** the inbound write API returns success and the current sequence number ends with `9999`
- **THEN** the system SHALL display the message `流水號為最後號，請到上一步變更` and SHALL not auto-increment to another sequence number

#### Scenario: Duplicate tracking number is rejected
- **WHEN** the inbound write API returns `errorCode = DUPLICATE_TRACKING_NO`
- **THEN** the system SHALL display the backend message, keep the current sequence number unchanged, and allow the operator to submit a different tracking number