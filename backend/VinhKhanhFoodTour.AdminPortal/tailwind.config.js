/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Components/**/*.{razor,html,js}",
    "./Pages/**/*.{razor,html,js}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#f0f9ff',
          500: '#0284c7',
          600: '#0369a1',
          700: '#0c63e4',
        }
      },
      fontFamily: {
        sans: ['ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto'],
      }
    },
  },
  plugins: [],
}
