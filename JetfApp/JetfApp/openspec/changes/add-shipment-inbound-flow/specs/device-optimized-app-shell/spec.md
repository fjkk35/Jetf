## ADDED Requirements

### Requirement: Splash screen SHALL validate app version before login
The system SHALL show the company logo during startup, display the current app version at the top-right corner, and call `GET /api/app/version-check` before allowing the user to enter the login screen.

#### Scenario: Version matches latest release
- **WHEN** the splash screen starts and the API returns the same `latestVersionCode` as the installed `versionCode`
- **THEN** the system shows the logo and version information, stores the version-check result, and navigates to the login screen without prompting for update

#### Scenario: Force update is required
- **WHEN** the splash screen receives a successful response where `forceUpdate` is `true` and `latestVersionCode` differs from the installed `versionCode`
- **THEN** the system SHALL display the backend message and APK download URL, and SHALL block navigation to the login screen until the user updates the app

#### Scenario: Update is optional
- **WHEN** the splash screen receives a successful response where `forceUpdate` is `false` and `latestVersionCode` differs from the installed `versionCode`
- **THEN** the system SHALL display the backend message, provide an option to continue using the current version, and allow navigation to the login screen if the user chooses to proceed

### Requirement: Interactive screens SHALL provide fixed bottom function keys
The system SHALL render a non-scrolling fixed bottom action bar on interactive screens that maps DT 40 function keys to the current screen actions.

#### Scenario: Bottom bar remains visible with input changes
- **WHEN** the user opens a screen with editable fields and the software keyboard appears or the content area changes height
- **THEN** the bottom action bar remains fixed to the bottom edge and its F3/F4 buttons remain tappable

#### Scenario: Settings screen function mapping
- **WHEN** the user is on the inbound settings screen
- **THEN** the bottom action bar SHALL show F3 as return/exit and F4 as next step

#### Scenario: Inbound work screen function mapping
- **WHEN** the user is on the inbound work screen
- **THEN** the bottom action bar SHALL show F3 as previous step and F4 as change location

### Requirement: The app SHALL support DT 40 runtime constraints
The system SHALL run on Android 9 devices while being developed against Android 10+ APIs, and SHALL support scan input delivered by DT 40 broadcast intents.

#### Scenario: App launches on supported device
- **WHEN** the application is installed on a DT 40 device running Android 9
- **THEN** the application SHALL launch without using unsupported API 29-only behavior

#### Scenario: Scan data arrives through broadcast
- **WHEN** the active operation screen receives a DT 40 scan broadcast with a decoded tracking number
- **THEN** the system SHALL route the scanned value to the focused business input flow without requiring manual paste