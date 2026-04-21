(function () {
    'use strict';

    // Key used to save user preferences in localStorage
    const STORAGE_KEY = 'glh-a11y-prefs';
    const BODY = document.body;

    // ── Preferences storage ───────────────────────────────────────────────────
    // Load the saved preferences object; return empty object if nothing is stored
    function loadPrefs() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); }
        catch { return {}; }
    }

    // Write the updated preferences object back to localStorage
    function savePrefs(prefs) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    }

    // ── Body class helpers ────────────────────────────────────────────────────
    // Remove all classes in a group, then add only the active one.
    // This keeps body classes mutually exclusive within each setting group.
    function applyBodyClass(groupClasses, activeClass) {
        groupClasses.forEach(c => BODY.classList.remove(c));
        if (activeClass) BODY.classList.add(activeClass);
    }

    // ── Apply all saved prefs on page load ────────────────────────────────────
    // Called once at startup so the page immediately reflects the user's settings
    function applyAllPrefs(prefs) {
        // Text size
        applyBodyClass(['a11y-font-md','a11y-font-lg','a11y-font-xl'], prefs.fontSize || '');
        // Line spacing between paragraphs
        applyBodyClass(['a11y-line-relaxed','a11y-line-loose'], prefs.lineSpacing || '');
        // Space between individual letters
        applyBodyClass(['a11y-letter-wide','a11y-letter-wider'], prefs.letterSpacing || '');
        // Space between words
        applyBodyClass(['a11y-word-wide','a11y-word-wider'], prefs.wordSpacing || '');
        // High contrast / invert / greyscale colour modes
        applyBodyClass(['a11y-high-contrast','a11y-invert','a11y-grayscale'], prefs.colourMode || '');
        // Colour saturation level
        applyBodyClass(['a11y-saturation-low','a11y-saturation-high'], prefs.saturation || '');
        // Toggle classes (boolean on/off)
        BODY.classList.toggle('a11y-reduce-motion', !!prefs.reduceMotion);
        BODY.classList.toggle('a11y-focus-ring',    !!prefs.focusRing);
        BODY.classList.toggle('a11y-dyslexia-font', !!prefs.dyslexiaFont);
        BODY.classList.toggle('a11y-large-cursor',  !!prefs.largeCursor);
        // TTS speed is stored but only read at runtime when TTS starts
    }

    // ── Sync the panel controls to match current prefs ────────────────────────
    // Run this each time the panel opens so the controls show the right values
    function syncPanelToPrefs(prefs) {
        // Helper to set a control's value or checked state from the prefs object
        const set = (id, val) => {
            const el = document.getElementById(id);
            if (el) { if (el.type === 'checkbox') el.checked = !!val; else el.value = val || ''; }
        };
        set('a11y-font-size',      prefs.fontSize      || '');
        set('a11y-line-spacing',   prefs.lineSpacing   || '');
        set('a11y-letter-spacing', prefs.letterSpacing || '');
        set('a11y-word-spacing',   prefs.wordSpacing   || '');
        set('a11y-colour-mode',    prefs.colourMode    || '');
        set('a11y-saturation',     prefs.saturation    || '');
        set('a11y-reduce-motion',  prefs.reduceMotion);
        set('a11y-focus-ring',     prefs.focusRing);
        set('a11y-dyslexia-font',  prefs.dyslexiaFont);
        set('a11y-large-cursor',   prefs.largeCursor);
        set('a11y-tts-toggle',     prefs.ttsEnabled);
        set('a11y-tts-speed',      prefs.ttsSpeed || 1);
        set('a11y-tts-voice',      prefs.ttsVoice || '');
        // Refresh the human-readable labels next to the sliders
        updateSliderLabel('a11y-font-size',  'a11y-font-size-val',
            { '': 'Default', 'a11y-font-md': 'Medium', 'a11y-font-lg': 'Large', 'a11y-font-xl': 'X-Large' });
        updateSliderLabel('a11y-tts-speed',  'a11y-tts-speed-val', null, v => v + '×');
    }

    // Update the text label next to a slider with either a mapped name or a formatted value
    function updateSliderLabel(sliderId, labelId, map, fmt) {
        const slider = document.getElementById(sliderId);
        const label  = document.getElementById(labelId);
        if (!slider || !label) return;
        const v = slider.value;
        label.textContent = map ? (map[v] || v) : (fmt ? fmt(v) : v);
    }

    // ── Text-to-speech ────────────────────────────────────────────────────────
    let ttsUtterance = null;

    // Read the page content aloud using the Web Speech API
    function startTTS(prefs) {
        if (!window.speechSynthesis) return;
        // Prefer reading the main content area; fall back to the whole body
        const text = document.querySelector('#main-content')?.innerText || document.body.innerText;
        if (!text) return;
        stopTTS(); // Cancel any speech already in progress
        ttsUtterance = new SpeechSynthesisUtterance(text.substring(0, 5000)); // Limit to 5000 chars
        ttsUtterance.rate = parseFloat(prefs.ttsSpeed || 1);
        // Apply the user's chosen voice if one is saved
        if (prefs.ttsVoice) {
            const voices = window.speechSynthesis.getVoices();
            const v = voices.find(x => x.name === prefs.ttsVoice);
            if (v) ttsUtterance.voice = v;
        }
        // Show the reading indicator bar
        const bar     = document.getElementById('a11y-tts-bar');
        const barText = document.getElementById('a11y-tts-bar-text');
        if (bar)     bar.classList.add('visible');
        if (barText) barText.textContent = '🔊 Reading page aloud…';
        // Hide the bar automatically when reading finishes
        ttsUtterance.onend = () => {
            if (bar) bar.classList.remove('visible');
        };
        window.speechSynthesis.speak(ttsUtterance);
    }

    // Stop any speech currently playing and hide the reading bar
    function stopTTS() {
        if (window.speechSynthesis) window.speechSynthesis.cancel();
        const bar = document.getElementById('a11y-tts-bar');
        if (bar) bar.classList.remove('visible');
        ttsUtterance = null;
    }

    // ── Voice selector ────────────────────────────────────────────────────────
    // Build the voice dropdown from the browser's available voices
    function populateVoices() {
        const sel = document.getElementById('a11y-tts-voice');
        if (!sel || !window.speechSynthesis) return;
        const voices = window.speechSynthesis.getVoices();
        sel.innerHTML = '<option value="">Default voice</option>';
        voices.forEach(v => {
            const opt = document.createElement('option');
            opt.value       = v.name;
            opt.textContent = `${v.name} (${v.lang})`;
            sel.appendChild(opt);
        });
        // Re-select the saved voice after rebuilding the list
        const prefs = loadPrefs();
        if (prefs.ttsVoice) sel.value = prefs.ttsVoice;
    }

    // ── Panel open / close ────────────────────────────────────────────────────
    function openPanel() {
        document.getElementById('a11y-panel')?.classList.add('open');
        document.getElementById('a11y-overlay')?.classList.add('open');
        document.getElementById('a11y-panel-close')?.focus(); // Move focus into the panel for keyboard users
        document.body.style.overflow = 'hidden'; // Prevent background scrolling while panel is open
        populateVoices();
        syncPanelToPrefs(loadPrefs());
    }

    function closePanel() {
        document.getElementById('a11y-panel')?.classList.remove('open');
        document.getElementById('a11y-overlay')?.classList.remove('open');
        document.body.style.overflow = '';
        document.getElementById('a11y-open-btn')?.focus(); // Return focus to the trigger button
    }

    // ── Initialise everything on DOM ready ────────────────────────────────────
    document.addEventListener('DOMContentLoaded', () => {
        const prefs = loadPrefs();
        applyAllPrefs(prefs); // Apply saved settings immediately so there is no flash of unstyled content

        // Open / close panel
        document.getElementById('a11y-open-btn')?.addEventListener('click', openPanel);
        document.getElementById('a11y-panel-close')?.addEventListener('click', closePanel);
        document.getElementById('a11y-overlay')?.addEventListener('click', closePanel);
        document.addEventListener('keydown', e => { if (e.key === 'Escape') closePanel(); });

        // ── Accordion category sections inside the panel ────────────────────
        // Only one section can be open at a time - clicking another closes the previous one
        document.querySelectorAll('.a11y-category__trigger').forEach(btn => {
            btn.addEventListener('click', () => {
                const isOpen = btn.getAttribute('aria-expanded') === 'true';
                // Collapse all sections first
                document.querySelectorAll('.a11y-category__trigger').forEach(b => {
                    b.setAttribute('aria-expanded', 'false');
                    const c = document.getElementById(b.getAttribute('aria-controls'));
                    if (c) c.hidden = true;
                });
                // Expand the clicked section only if it was not already open
                if (!isOpen) {
                    btn.setAttribute('aria-expanded', 'true');
                    const content = document.getElementById(btn.getAttribute('aria-controls'));
                    if (content) content.hidden = false;
                }
            });
        });

        // ── Binding helpers ─────────────────────────────────────────────────

        // Bind a <select> element so changing it saves the value and applies the right body class
        function bindSelect(id, prefKey, groupClasses, getValue) {
            const el = document.getElementById(id);
            if (!el) return;
            el.addEventListener('change', () => {
                const prefs = loadPrefs();
                const val = getValue ? getValue(el) : el.value;
                prefs[prefKey] = val;
                savePrefs(prefs);
                if (groupClasses) applyBodyClass(groupClasses, val);
                else BODY.classList.toggle(val, !!val);
            });
        }

        // Bind a checkbox element so toggling it saves the boolean and applies the body class
        function bindToggle(id, prefKey, className) {
            const el = document.getElementById(id);
            if (!el) return;
            el.addEventListener('change', () => {
                const prefs = loadPrefs();
                prefs[prefKey] = el.checked;
                savePrefs(prefs);
                BODY.classList.toggle(className, el.checked);
            });
        }

        // ── Visual settings ──────────────────────────────────────────────────
        bindSelect('a11y-font-size',    'fontSize',    ['a11y-font-md','a11y-font-lg','a11y-font-xl']);
        // Also update the human-readable label when font size changes
        document.getElementById('a11y-font-size')?.addEventListener('change', () =>
            updateSliderLabel('a11y-font-size','a11y-font-size-val',
                {'':'Default','a11y-font-md':'Medium','a11y-font-lg':'Large','a11y-font-xl':'X-Large'}));
        bindSelect('a11y-colour-mode',  'colourMode',  ['a11y-high-contrast','a11y-invert','a11y-grayscale']);
        bindSelect('a11y-saturation',   'saturation',  ['a11y-saturation-low','a11y-saturation-high']);

        // ── Cognitive settings ───────────────────────────────────────────────
        bindSelect('a11y-line-spacing',   'lineSpacing',   ['a11y-line-relaxed','a11y-line-loose']);
        bindSelect('a11y-letter-spacing', 'letterSpacing', ['a11y-letter-wide','a11y-letter-wider']);
        bindSelect('a11y-word-spacing',   'wordSpacing',   ['a11y-word-wide','a11y-word-wider']);
        bindToggle('a11y-dyslexia-font',  'dyslexiaFont',  'a11y-dyslexia-font');

        // ── Neurological settings ────────────────────────────────────────────
        bindToggle('a11y-reduce-motion', 'reduceMotion', 'a11y-reduce-motion');
        bindToggle('a11y-focus-ring',    'focusRing',    'a11y-focus-ring');

        // ── Physical settings ────────────────────────────────────────────────
        bindToggle('a11y-large-cursor', 'largeCursor', 'a11y-large-cursor');

        // ── Auditory / TTS settings ──────────────────────────────────────────
        // Toggle TTS on/off
        document.getElementById('a11y-tts-toggle')?.addEventListener('change', (e) => {
            const prefs = loadPrefs();
            prefs.ttsEnabled = e.target.checked;
            savePrefs(prefs);
            if (e.target.checked) startTTS(prefs);
            else stopTTS();
        });

        // Save the chosen reading speed and update the label
        document.getElementById('a11y-tts-speed')?.addEventListener('input', (e) => {
            const prefs = loadPrefs();
            prefs.ttsSpeed = e.target.value;
            savePrefs(prefs);
            const lbl = document.getElementById('a11y-tts-speed-val');
            if (lbl) lbl.textContent = e.target.value + '×';
        });

        // Save the chosen voice so it persists across page loads
        document.getElementById('a11y-tts-voice')?.addEventListener('change', (e) => {
            const prefs = loadPrefs();
            prefs.ttsVoice = e.target.value;
            savePrefs(prefs);
        });

        document.getElementById('a11y-tts-stop')?.addEventListener('click', stopTTS);

        // ── Reset button ─────────────────────────────────────────────────────
        // Wipe all saved preferences and remove all accessibility body classes
        document.getElementById('a11y-reset')?.addEventListener('click', () => {
            localStorage.removeItem(STORAGE_KEY);
            [
                'a11y-font-md','a11y-font-lg','a11y-font-xl',
                'a11y-line-relaxed','a11y-line-loose',
                'a11y-letter-wide','a11y-letter-wider',
                'a11y-word-wide','a11y-word-wider',
                'a11y-high-contrast','a11y-invert','a11y-grayscale',
                'a11y-saturation-low','a11y-saturation-high',
                'a11y-reduce-motion','a11y-focus-ring',
                'a11y-dyslexia-font','a11y-large-cursor'
            ].forEach(c => BODY.classList.remove(c));
            stopTTS();
            syncPanelToPrefs({}); // Reset all panel controls to their default state
        });

        // ── Voice list population ────────────────────────────────────────────
        // Browsers load voices asynchronously; onvoiceschanged fires once they are ready
        if (window.speechSynthesis) {
            window.speechSynthesis.onvoiceschanged = populateVoices;
            populateVoices(); // Also try immediately in case voices are already loaded
        }
    });
})();
