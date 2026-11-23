/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#EEF2FF',
          100: '#E0E7FF',
          200: '#C7D2FE',
          300: '#A5B4FC',
          400: '#818CF8',
          500: '#6366F1',
          600: '#4F46E5',
          700: '#4338CA', // Deep Indigo primary
          800: '#3730A3',
          900: '#312E81',
          DEFAULT: '#4338CA',
        },
        secondary: {
          50: '#EFF6FF',
          100: '#DBEAFE',
          500: '#3B82F6',
          600: '#2563EB', // Blue secondary
          700: '#1D4ED8',
          DEFAULT: '#2563EB',
        },
        success: {
          50: '#F0FDF4',
          100: '#DCFCE7',
          500: '#22C55E',
          600: '#16A34A', // Green success
          700: '#15803D',
          DEFAULT: '#16A34A',
        },
        warning: {
          50: '#FFFBEB',
          100: '#FEF3C7',
          500: '#F59E0B',
          600: '#D97706', // Amber warning
          700: '#B45309',
          DEFAULT: '#D97706',
        },
        danger: {
          50: '#FEF2F2',
          100: '#FEE2E2',
          500: '#EF4444',
          600: '#DC2626', // Red danger
          700: '#B91C1C',
          DEFAULT: '#DC2626',
        },
        surface: '#FFFFFF',
        background: '#F8FAFC',
        slate: {
          900: '#0F172A',
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
      },
    },
  },
  plugins: [],
}

