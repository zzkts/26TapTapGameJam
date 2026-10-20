# UI alignment and slider polish — 2026-10-20

Applied and saved to BeginScene and GameScene in Unity 6000.3.8f1.

- Settings uses a centered 620 × 680 card with consistent 54-unit side padding. Labels and percentages share rows; buttons share edges. Fullscreen now has explicit state text and a switch thumb.
- Slider fill and track use the same rectangle, replacing the old 2.5% horizontal fill-area inset. A centered, zero-height handle container prevents Unity Slider from vertically stretching its 24 × 24 circular thumb. The interaction region remains 44 units tall.
- Pause menu uses matching card dimensions and aligned buttons. Gene confirmation shortcuts stay in the footer. Main-menu title, controls and jam credit offsets are corrected. Dialogue skip stays inside the dialogue card, away from the objective panel.
- Existing saved sprites and font are reused. No new visual textures or gameplay changes were introduced. Both scene migrations check non-UI transform and collider-count signatures before and after.

## Verification

56 opt-in Unity Play Mode checks passed (Validation.json), including slider values 0%, 1%, 67%, 100% in both scenes, circular thumb geometry, label rows, music credits, pause layout, gene terminal opening, dialogue skip placement and return to menu. Screenshots below are native Game view captures, not mockups.

- Settings-67.png — settings at 67%.
- Settings-Endpoints.png — empty and full slider endpoints.
- Main-Menu.png, Pause.png, Gene-Workbench.png, Story-Dialogue.png, Music-Credits.png.

Card bounds were checked mathematically against the existing CanvasScaler Expand behavior at 1920 × 1080, 1280 × 720 and 1024 × 768; native screenshots use the current 1920 × 1080 Game view.

Post-run scene/prefab reference validation passed for both build scenes, 19 prefabs and 366 sprite renderers. Editor returned to BeginScene, stopped and clean. The checks used isolated in-memory progress and confirmed the real save, ending history and stored volume preferences were unchanged. Test volume adjustments were restored in memory. No build was produced.

The tests and request runner are Editor-only, opt-in utilities. To repeat, stop Play Mode, save scene edits, and create Temp/UIAlign/test-request.txt. Temp/UIAlign/apply-request.txt reruns only the UI migration. Wait for each result before creating another request.
