## 1. Project setup and configuration

- [x] 1.1 Update Android build configuration for DT 40 compatibility, build-time API settings, and required dependencies.
- [x] 1.2 Add shared app resources including the company logo, theme updates, strings, and base layouts for fixed bottom actions.

## 2. Core architecture and networking

- [x] 2.1 Implement app configuration, API models, Retrofit service definitions, repository layer, and HMAC signature support.
- [x] 2.2 Implement sequence validation/increment utilities and shared UI state models for login and inbound workflow.

## 3. Flow screens and interaction logic

- [x] 3.1 Implement MainActivity shell with fixed bottom function keys, scan broadcast dispatching, and fragment navigation.
- [x] 3.2 Implement Splash, Login, Menu, Inbound Settings, and Inbound Work fragments with DT 40-oriented layouts and validation.

## 4. Business workflow integration

- [x] 4.1 Connect version check, login, source type loading, shipment check, and inbound write actions to the UI state flow.
- [x] 4.2 Implement unknown-shipment confirmation, conditional return tracking field, location lock/unlock, and sequence advancement rules.

## 5. Verification

- [x] 5.1 Add or adjust focused unit tests for sequence utilities and workflow validation logic where practical.
- [x] 5.2 Build the app or run relevant Gradle validation to confirm the project compiles after the change.