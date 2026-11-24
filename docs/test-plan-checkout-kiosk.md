# M2 Integration & Usability Test Plan — Checkout Area

**Owner:** Bashiru · **Milestone:** M2 (Nov 27, 2026) · **Modules:** CheckoutSensor, CheckoutStationController, KioskPurchaseSimulator

This section of the M2 test plan covers how the checkout modules work with each
other and with the rest of SmartShop. Unit tests for each class already exist in
`SmartShop.Tests/Devices/` and `SmartShop.Tests/Services/`. The automated
integration cases below are in
`SmartShop.Tests/Integration/CheckoutKioskIntegrationTests.cs` (run with `dotnet test`).

## 1. Scope

| In scope | Out of scope |
|---|---|
| Kiosk → ShelfStock → CheckoutSensor purchase flow | Real payment, cart, customer accounts (excluded by REQ-KSK-001) |
| Shipped looped data files (`checkout_sensor.txt`, `checkout_station.txt`) | Physical checkout hardware |
| Lane open/close commands, admin check, manual override | Other members' devices (covered in their sections) |
| Checkout tiles and kiosk screen on the HMI dashboard (usability) | |

## 2. Dependencies on other modules

| Dependency | Owner | Status at M2 | How it is tested now |
|---|---|---|---|
| `ShelfStockSensor` implementing `IShelfStock` | Weiming | On branch `feature/t16-refrigerator-shelfstock`, not yet on main | In-memory `IShelfStock` stand-in; rerun IT-KSK-01–05 with the real sensor once merged |
| `ThresholdEngine` (Normal / Warning / Critical) | Weiming | Merged | Used directly by CheckoutSensor |
| RBAC Authentication | Farhiya | Pending | Controller rejects commands without an admin user ID; full role test is IT-RBAC-01 below |
| LoggingService (EventLog / AuditTrail) | Farhiya / Kristyna | Schema merged, service pending | Kiosk raises `PurchaseCompleted`; controller keeps `LastPreviousState`; log write test is IT-LOG-01 below |
| HMI dashboard | Olawale | In progress | Usability cases UT-01 to UT-06 |

## 3. Automated integration test cases

| ID | Test | Steps | Expected result | Req. |
|---|---|---|---|---|
| IT-CHK-01 | Shipped sensor file is valid | Read every record of `Data/checkout_sensor.txt` | Every record is accepted; Normal, Warning and Critical all occur | REQ-SYS-010, REQ-STS-001 |
| IT-CHK-02 | Sensor file loops | Read all records, then one more | Last read equals the first record's value | REQ-TEL-001 |
| IT-CSC-01 | Shipped station file is valid | Read every record of `Data/checkout_station.txt` | Every record is accepted; both Open and Closed occur | REQ-SYS-010 |
| IT-KSK-01 | Purchase updates both devices | Sensor at 5, shelf has 3 milk; buy 1 milk | Shelf → 2, sensor → 6, status Warning, 1 transaction | REQ-KSK-002, REQ-INT-KSK-001 |
| IT-KSK-02 | Out-of-stock purchase | Shelf has 1; buy 2 | Fails with message; shelf, sensor and log unchanged | REQ-KSK-002, REQ-USA-KSK-001 |
| IT-KSK-03 | Purchase event for logging | Buy 3 bread at station 2 | One `PurchaseCompleted` event: BREAD, −3, station 2 | REQ-LOG-001 |
| IT-KSK-04 | Simulation resumes after purchase | Sensor 3 → buy → next record 1 | Value 4 after purchase, 1 after next read; count kept | REQ-TEL-001 |
| IT-KSK-05 | Purchase while sensor file missing | Missing file; buy 1 | No crash; purchase recorded; sensor available | REQ-NFR-REL-001 |
| IT-CSC-02 | Lane command without admin | Send Closed with empty admin ID | Rejected; telemetry still 1 (Open); no override | REQ-AUTH-001 |
| IT-CSC-03 | Admin override and resume | File says Open; admin sends Closed; read; resume; read | Closed held while overridden (telemetry 0); Open after resume | REQ-CTL-005 |
| IT-CSC-04 | Device does not confirm | Command with simulated failure | Rejected; lane still Open; no override | REQ-CTL-007 |

## 4. Integration cases to run once dependencies merge

| ID | Test | Expected result |
|---|---|---|
| IT-SHF-01 | Rerun IT-KSK-01–05 with the real `ShelfStockSensor` | Same results; shelf count on the dashboard drops by the purchased quantity |
| IT-RBAC-01 | Lane command from a logged-in non-admin role | Rejected before reaching the controller; AuditTrail records the refusal |
| IT-LOG-01 | Purchase, then lane command | EventLog has the purchase; AuditTrail has the command with previous and new state, admin ID and UTC time |
| IT-HMI-01 | Dashboard polling | Checkout tile updates each poll and loops through the file without stopping |

## 5. Usability test cases (manual, HMI dashboard and kiosk screen)

Run with two or three classmates who have not seen the app. Record time taken and any hesitation.

| ID | Task given to tester | Pass criteria | Req. |
|---|---|---|---|
| UT-01 | "Buy one item from the kiosk screen." | Done in one click, under 10 s, without help | REQ-KSK-001 |
| UT-02 | "Did the purchase work? What changed?" | Tester reads the confirmation and finds the stock and checkout changes | REQ-USA-KSK-001 |
| UT-03 | "Try to buy an item that is out of stock." | Tester understands the error message and nothing else changes | REQ-USA-KSK-001 |
| UT-04 | "Is the checkout area busy right now?" | Tester reads the checkout tile's value and Normal / Warning / Critical colour correctly | REQ-STS-001 |
| UT-05 | "Close checkout lane 1." (as admin) | Done in ≤ 3 clicks; lane tile shows Closed only after confirmation | REQ-CTL-005, REQ-CTL-007 |
| UT-06 | "Close the lane" (as a non-admin) | Button is unavailable or a clear "not allowed" message is shown | REQ-AUTH-001 |

## 6. Entry and exit criteria

- **Entry:** branch builds with no errors; all unit tests pass.
- **Exit for M2:** all section 3 cases pass in CI or locally; section 4 and 5 cases scheduled for M3 test report.
