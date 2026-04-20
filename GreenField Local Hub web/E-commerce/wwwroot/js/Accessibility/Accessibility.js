(function () {
    'use strict';

    const STORAGE_KEY = 'glh-a11y-prefs';
    const BODY = document.body;

    // Load saved prefs
    function loadPrefs() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); }
        catch { return {}; }
    }
    function savePrefs(prefs) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    }

    // Apply a body class exclusively within a group
    function applyBodyClass(groupClasses, activeClass) {
        groupClasses.forEach(c => BODY.classList.remove(c));
        if (activeClass) BODY.classList.add(activeClass);
    }

    // Apply all saved prefs on load
    function applyAllPrefs(prefs) {
        // Font size
        applyBodyClass(['a11y-font-md','a11y-font-lg','a11y-font-xl'], prefs.fontSize || '');
        // Line spacing
        applyBodyClass(['a11y-line-relaxed','a11y-line-loose'], prefs.lineSpacing || '');
        // Letter spacing
        applyBodyClass(['a11y-letter-wide','a11y-letter-wider'], prefs.letterSpacing || '');
        // Word spacing
        applyBodyClass(['a11y-word-wide','a11y-word-wider'], prefs.wordSpacing || '');
        // Contrast/colour
        applyBodyClass(['a11y-high-contrast','a11y-invert','a11y-grayscale'], prefs.colourMode || '');
        // Saturation
        applyBodyClass(['a11y-saturation-low','a11y-saturation-high'], prefs.saturation || '');
        // Reduce motion
        BODY.classList.toggle('a11y-reduce-motion', !!prefs.reduceMotion);
        // Focus ring
        BODY.classList.toggle('a11y-focus-ring', !!prefs.focusRing);
        // Dyslexia font
        BODY.classList.toggle('a11y-dyslexia-font', !!prefs.dyslexiaFont);
        // Large cursor
        BODY.classList.toggle('a11y-large-cursor', !!prefs.largeCursor);
        // TTS speed stored for runtime use
    }

    // Sync panel controls to current prefs
    function syncPanelToPrefs(prefs) {
        const set = (id, val) => { const el = document.getElementById(id); if (el) { if (el.type === 'checkbox') el.checked = !!val; else el.value = val || ''; } };
        set('a11y-font-size', prefs.fontSize || '');
        set('a11y-line-spacing', prefs.lineSpacing || '');
        set('a11y-letter-spacing', prefs.letterSpacing || '');
        set('a11y-word-spacing', prefs.wordSpacing || '');
        set('a11y-colour-mode', prefs.colourMode || '');
        set('a11y-saturation', prefs.saturation || '');
        set('a11y-reduce-motion', prefs.reduceMotion);
        set('a11y-focus-ring', prefs.focusRing);
        set('a11y-dyslexia-font', prefs.dyslexiaFont);
        set('a11y-large-cursor', prefs.largeCursor);
        set('a11y-tts-toggle', prefs.ttsEnabled);
        set('a11y-tts-speed', prefs.ttsSpeed || 1);
        set('a11y-tts-voice', prefs.ttsVoice || '');
        updateSliderLabel('a11y-font-size', 'a11y-font-size-val', { '': 'Default', 'a11y-font-md': 'Medium', 'a11y-font-lg': 'Large', 'a11y-font-xl': 'X-Large' });
        updateSliderLabel('a11y-tts-speed', 'a11y-tts-speed-val', null, v => v + '×');
    }

    function updateSliderLabel(sliderId, labelId, map, fmt) {
        const slider = document.getElementById(sliderId);
        const label = document.getElementById(labelId);
        if (!slider || !label) return;
        const v = slider.value;
        label.textContent = map ? (map[v] || v) : (fmt ? fmt(v) : v);
    }

    // TTS
    let ttsUtterance = null;
    function startTTS(prefs) {
        if (!window.speechSynthesis) return;
        const text = document.querySelector('#main-content')?.innerText || document.body.innerText;
        if (!text) return;
        stopTTS();
        ttsUtterance = new SpeechSynthesisUtterance(text.substring(0, 5000));
        ttsUtterance.rate = parseFloat(prefs.ttsSpeed || 1);
        // Set voice
        if (prefs.ttsVoice) {
            const voices = window.speechSynthesis.getVoices();
            const v = voices.find(x => x.name === prefs.ttsVoice);
            if (v) ttsUtterance.voice = v;
        }
        const bar = document.getElementById('a11y-tts-bar');
        const barText = document.getElementById('a11y-tts-bar-text');
        if (bar) bar.classList.add('visible');
        if (barText) barText.textContent = '🔊 Reading page aloud…';
        ttsUtterance.onend = () => {
            if (bar) bar.classList.remove('visible');
        };
        window.speechSynthesis.speak(ttsUtterance);
    }
    function stopTTS() {
        if (window.speechSynthesis) window.speechSynthesis.cancel();
        const bar = document.getElementById('a11y-tts-bar');
        if (bar) bar.classList.remove('visible');
        ttsUtterance = null;
    }

    // Populate voice selector
    function populateVoices() {
        const sel = document.getElementById('a11y-tts-voice');
        if (!sel || !window.speechSynthesis) return;
        const voices = window.speechSynthesis.getVoices();
        sel.innerHTML = '<option value="">Default voice</option>';
        voices.forEach(v => {
            const opt = document.createElement('option');
            opt.value = v.name;
            opt.textContent = `${v.name} (${v.lang})`;
            sel.appendChild(opt);
        });
        const prefs = loadPrefs();
        if (prefs.ttsVoice) sel.value = prefs.ttsVoice;
    }

    // Panel open/close
    function openPanel() {
        document.getElementById('a11y-panel')?.classList.add('open');
        document.getElementById('a11y-overlay')?.classList.add('open');
        document.getElementById('a11y-panel-close')?.focus();
        document.body.style.overflow = 'hidden';
        populateVoices();
        syncPanelToPrefs(loadPrefs());
    }
    function closePanel() {
        document.getElementById('a11y-panel')?.classList.remove('open');
        document.getElementById('a11y-overlay')?.classList.remove('open');
        document.body.style.overflow = '';
        document.getElementById('a11y-open-btn')?.focus();
    }

    // Initialise on DOM ready
    document.addEventListener('DOMContentLoaded', () => {
        const prefs = loadPrefs();
        applyAllPrefs(prefs);

        // Open/close
        document.getElementById('a11y-open-btn')?.addEventListener('click', openPanel);
        document.getElementById('a11y-panel-close')?.addEventListener('click', closePanel);
        document.getElementById('a11y-overlay')?.addEventListener('click', closePanel);
        document.addEventListener('keydown', e => { if (e.key === 'Escape') closePanel(); });

        // Accordion categories
        document.querySelectorAll('.a11y-category__trigger').forEach(btn => {
            btn.addEventListener('click', () => {
                const isOpen = btn.getAttribute('aria-expanded') === 'true';
                // Close all
                document.querySelectorAll('.a11y-category__trigger').forEach(b => {
                    b.setAttribute('aria-expanded', 'false');
                    const c = document.getElementById(b.getAttribute('aria-controls'));
                    if (c) c.hidden = true;
                });
                if (!isOpen) {
                    btn.setAttribute('aria-expanded', 'true');
                    const content = document.getElementById(btn.getAttribute('aria-controls'));
                    if (content) content.hidden = false;
                }
            });
        });

        // Helper to bind a select/checkbox change to prefs
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

        // Visual
        bindSelect('a11y-font-size', 'fontSize', ['a11y-font-md','a11y-font-lg','a11y-font-xl']);
        document.getElementById('a11y-font-size')?.addEventListener('change', () => updateSliderLabel('a11y-font-size','a11y-font-size-val',{'':'Default','a11y-font-md':'Medium','a11y-font-lg':'Large','a11y-font-xl':'X-Large'}));
        bindSelect('a11y-colour-mode', 'colourMode', ['a11y-high-contrast','a11y-invert','a11y-grayscale']);
        bindSelect('a11y-saturation', 'saturation', ['a11y-saturation-low','a11y-saturation-high']);

        // Cognitive
        bindSelect('a11y-line-spacing', 'lineSpacing', ['a11y-line-relaxed','a11y-line-loose']);
        bindSelect('a11y-letter-spacing', 'letterSpacing', ['a11y-letter-wide','a11y-letter-wider']);
        bindSelect('a11y-word-spacing', 'wordSpacing', ['a11y-word-wide','a11y-word-wider']);
        bindToggle('a11y-dyslexia-font', 'dyslexiaFont', 'a11y-dyslexia-font');

        // Neurological
        bindToggle('a11y-reduce-motion', 'reduceMotion', 'a11y-reduce-motion');
        bindToggle('a11y-focus-ring', 'focusRing', 'a11y-focus-ring');

        // Physical
        bindToggle('a11y-large-cursor', 'largeCursor', 'a11y-large-cursor');

        // Auditory - TTS
        document.getElementById('a11y-tts-toggle')?.addEventListener('change', (e) => {
            const prefs = loadPrefs();
            prefs.ttsEnabled = e.target.checked;
            savePrefs(prefs);
            if (e.target.checked) startTTS(prefs);
            else stopTTS();
        });
        document.getElementById('a11y-tts-speed')?.addEventListener('input', (e) => {
            const prefs = loadPrefs();
            prefs.ttsSpeed = e.target.value;
            savePrefs(prefs);
            const lbl = document.getElementById('a11y-tts-speed-val');
            if (lbl) lbl.textContent = e.target.value + '×';
        });
        document.getElementById('a11y-tts-voice')?.addEventListener('change', (e) => {
            const prefs = loadPrefs();
            prefs.ttsVoice = e.target.value;
            savePrefs(prefs);
        });
        document.getElementById('a11y-tts-stop')?.addEventListener('click', stopTTS);

        // Reset
        document.getElementById('a11y-reset')?.addEventListener('click', () => {
            localStorage.removeItem(STORAGE_KEY);
            ['a11y-font-md','a11y-font-lg','a11y-font-xl',
             'a11y-line-relaxed','a11y-line-loose',
             'a11y-letter-wide','a11y-letter-wider',
             'a11y-word-wide','a11y-word-wider',
             'a11y-high-contrast','a11y-invert','a11y-grayscale',
             'a11y-saturation-low','a11y-saturation-high',
             'a11y-reduce-motion','a11y-focus-ring',
             'a11y-dyslexia-font','a11y-large-cursor'
            ].forEach(c => BODY.classList.remove(c));
            stopTTS();
            syncPanelToPrefs({});
        });

        // Populate voices once available
        if (window.speechSynthesis) {
            window.speechSynthesis.onvoiceschanged = populateVoices;
            populateVoices();
        }
    });
})();
