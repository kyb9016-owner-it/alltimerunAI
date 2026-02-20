# Player Flows

## F1: First Session (Happy Path)
1. Splash
2. Home
3. Tap `Start`
4. In-Run (move, dodge, collect)
5. Fail
6. Result
7. Tap `Retry`
8. In-Run again

## F2: Continue With Reward Ad
1. Fail
2. Continue modal (`Watch Ad to Continue`)
3. Reward ad success
4. Resume In-Run with short shield
5. Fail or finish
6. Result

## F3: Interstitial Between Runs
1. Result
2. Tap `Home` or `Next`
3. Interstitial check
4. If eligible, show ad
5. Home

## F4: IAP Remove Ads
1. Home -> Shop
2. Tap `Remove Ads`
3. Store purchase success
4. Flag persisted
5. Interstitial disabled (reward ads remain optional)

## Error States
- Ad load fail: show toast and fallback to normal retry.
- Purchase fail/cancel: show non-blocking message, keep shop open.
- Offline: disable ad/purchase buttons with tooltip.
