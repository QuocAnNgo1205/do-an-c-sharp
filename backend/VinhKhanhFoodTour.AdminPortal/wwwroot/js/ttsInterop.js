window.audioInterop = {
    currentAudio: null,

    // Ánh xạ mã ngôn ngữ ngắn → BCP-47 chuẩn (cho Web Speech API)
    langMap: {
        'vi': 'vi-VN', 'en': 'en-US', 'ja': 'ja-JP',
        'ko': 'ko-KR', 'zh': 'zh-CN', 'fr': 'fr-FR', 'de': 'de-DE', 'es': 'es-ES'
    },

    playAudioFile: function (url) {
        this.stopAudio();
        try {
            this.currentAudio = new Audio(url);
            this.currentAudio.play();
            return true;
        } catch (e) {
            console.error('[audioInterop] Lỗi khi phát audio file:', e);
            return false;
        }
    },

    /**
     * Phát TTS với giọng đúng ngôn ngữ.
     * Chiến lược:
     *   1. Thử Web Speech API với giọng có sẵn trên thiết bị
     *   2. Nếu không có giọng phù hợp → dùng Google Translate TTS (audio URL)
     */
    playTextToSpeech: function (text, langCode) {
        this.stopAudio();

        const targetBcp47 = this.langMap[langCode] || langCode;

        const tryNativeTTS = (voices) => {
            // Tìm giọng phù hợp: exact match → prefix match → bất kỳ giọng có tên ngôn ngữ
            const voice = voices.find(v => v.lang === targetBcp47)
                       || voices.find(v => v.lang.startsWith(langCode))
                       || voices.find(v => v.lang.toLowerCase().startsWith(langCode.toLowerCase()));

            if (voice) {
                console.log(`[audioInterop] ✅ Native voice: ${voice.name} (${voice.lang})`);
                const utterance = new SpeechSynthesisUtterance(text);
                utterance.lang   = targetBcp47;
                utterance.voice  = voice;
                utterance.pitch  = 1.0;
                utterance.rate   = 0.95;
                utterance.volume = 1.0;
                window.speechSynthesis.speak(utterance);
                return true;
            }

            // Không tìm được giọng bản địa → dùng Google Translate TTS
            console.warn(`[audioInterop] ⚠️ No '${targetBcp47}' voice on device. Falling back to Google TTS.`);
            return this.playGoogleTTS(text, langCode);
        };

        if (!window.speechSynthesis) {
            // Trình duyệt không hỗ trợ Web Speech → thẳng sang Google TTS
            return this.playGoogleTTS(text, langCode);
        }

        const voices = window.speechSynthesis.getVoices();
        if (voices && voices.length > 0) {
            return tryNativeTTS(voices);
        }

        // Chrome load voices bất đồng bộ - đợi event
        window.speechSynthesis.onvoiceschanged = () => {
            window.speechSynthesis.onvoiceschanged = null;
            tryNativeTTS(window.speechSynthesis.getVoices());
        };

        // Timeout fallback sau 1.5 giây nếu event không bắn
        setTimeout(() => {
            const v = window.speechSynthesis.getVoices();
            if (v && v.length > 0) {
                window.speechSynthesis.onvoiceschanged = null;
                tryNativeTTS(v);
            } else {
                this.playGoogleTTS(text, langCode);
            }
        }, 1500);

        return true;
    },

    /**
     * Phát TTS qua Google Translate audio endpoint.
     * Hoạt động dưới dạng HTML5 Audio nên không bị CORS.
     * Giới hạn ~200 ký tự / lần (do URL length limit).
     */
    playGoogleTTS: function (text, langCode) {
        // Cắt tối đa 200 ký tự (giới hạn của Google TTS endpoint)
        const chunk = text.length > 200 ? text.substring(0, 197) + '...' : text;
        const url   = `https://translate.google.com/translate_tts?ie=UTF-8&q=${encodeURIComponent(chunk)}&tl=${langCode}&client=tw-ob`;

        console.log(`[audioInterop] 🌐 Google TTS: ${langCode} → "${chunk.substring(0, 50)}..."`);

        try {
            this.currentAudio = new Audio(url);
            this.currentAudio.onerror = (e) => {
                console.error('[audioInterop] Google TTS failed:', e);
            };
            this.currentAudio.play();
            return true;
        } catch (e) {
            console.error('[audioInterop] Google TTS error:', e);
            return false;
        }
    },

    stopAudio: function () {
        if (this.currentAudio) {
            try {
                this.currentAudio.pause();
                this.currentAudio.currentTime = 0;
            } catch (e) { }
            this.currentAudio = null;
        }
        if (window.speechSynthesis && window.speechSynthesis.speaking) {
            window.speechSynthesis.cancel();
        }
    }
};

