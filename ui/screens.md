# Screens

## Splash
- Purpose: quick boot and preload.
- Elements: logo, loading spinner.
- States: loading, timeout(error -> retry).

## Home
- Purpose: entry and high-level progression.
- Elements: Start button, coin balance, best score, shop button, settings button.
- Interactions: Start -> In-Run, Shop -> Shop, Settings -> Settings.

## In-Run HUD
- Purpose: gameplay information.
- Elements: score, coins, pause, optional power-up icon.
- States: normal, paused, low-health warning.

## Fail/Continue Modal
- Purpose: recover session with rewarded ad.
- Elements: Continue(Ad), No Thanks.
- States: ad ready, ad unavailable(disable continue).

## Result
- Purpose: post-run summary and loop control.
- Elements: run score, earned coins, Retry, Home, optional Next.
- States: normal, interstitial pending.

## Shop
- Purpose: monetization and progression.
- Elements: Remove Ads IAP, coin packs, restore purchases.
- States: loading products, success, failed transaction.

## Settings
- Purpose: player preferences.
- Elements: sound toggle, vibration toggle, privacy/legal links.
- States: normal, reset confirmation.
