window.audioInterop = {
    currentAudio: null,
    
    playAudioFile: function (url) {
        this.stopAudio(); // Stop any currently playing audio or TTS
        
        try {
            this.currentAudio = new Audio(url);
            this.currentAudio.play();
            return true;
        } catch (e) {
            console.error("Lỗi khi phát audio file:", e);
            return false;
        }
    },
    
    playTextToSpeech: function (text, langCode) {
        this.stopAudio(); // Stop any currently playing audio or TTS
        
        if (!window.speechSynthesis) {
            console.error("Trình duyệt không hỗ trợ Web Speech API.");
            return false;
        }

        try {
            const utterance = new SpeechSynthesisUtterance(text);
            
            // Map common short lang codes to standard TTS BCP-47 identifiers
            const langMap = {
                'vi': 'vi-VN',
                'en': 'en-US',
                'ja': 'ja-JP',
                'ko': 'ko-KR',
                'zh': 'zh-CN',
                'fr': 'fr-FR'
            };
            
            utterance.lang = langMap[langCode] || langCode;
            
            // Try to select an appropriate voice
            const voices = window.speechSynthesis.getVoices();
            if (voices && voices.length > 0) {
                const preferredVoice = voices.find(v => v.lang.startsWith(utterance.lang) || v.lang.startsWith(langCode));
                if (preferredVoice) {
                    utterance.voice = preferredVoice;
                }
            }
            
            window.speechSynthesis.speak(utterance);
            return true;
        } catch (e) {
            console.error("Lỗi khi phát TTS:", e);
            return false;
        }
    },
    
    stopAudio: function () {
        // Pause and reset HTML5 Audio
        if (this.currentAudio) {
            try {
                this.currentAudio.pause();
                this.currentAudio.currentTime = 0;
            } catch (e) { }
            this.currentAudio = null;
        }
        
        // Stop TTS
        if (window.speechSynthesis && window.speechSynthesis.speaking) {
            window.speechSynthesis.cancel();
        }
    }
};

// Ensure voices are loaded (some browsers load asynchronously)
if (window.speechSynthesis && window.speechSynthesis.onvoiceschanged !== undefined) {
    window.speechSynthesis.onvoiceschanged = function() {
        window.speechSynthesis.getVoices();
    };
}
