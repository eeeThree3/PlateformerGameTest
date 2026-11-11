# Project Guidelines

- Never hide or disable existing Unity scene or prefab objects unless the user explicitly requests it. Preserve their active and visible state; do not change `SetActive`, `m_IsActive`, renderer visibility, or equivalent settings as a workaround.
