import React, { createContext, useContext, useState, useEffect } from 'react';
import en from '../i18n/en.json';
import hi from '../i18n/hi.json';

const translations = { en, hi };

const LanguageContext = createContext();

export function LanguageProvider({ children }) {
  const [language, setLanguage] = useState(() => {
    return localStorage.getItem('smartstock_lang') || 'en';
  });

  useEffect(() => {
    localStorage.setItem('smartstock_lang', language);
  }, [language]);

  const t = (path) => {
    const keys = path.split('.');
    let current = translations[language];
    for (const key of keys) {
      if (!current || current[key] === undefined) {
        // fallback to english
        let fallback = translations['en'];
        for (const fbKey of keys) {
          if (!fallback || fallback[fbKey] === undefined) return path;
          fallback = fallback[fbKey];
        }
        return fallback;
      }
      current = current[key];
    }
    return current;
  };

  const toggleLanguage = () => {
    setLanguage((prev) => (prev === 'en' ? 'hi' : 'en'));
  };

  return (
    <LanguageContext.Provider value={{ language, setLanguage, toggleLanguage, t }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error('useLanguage must be used within a LanguageProvider');
  }
  return context;
}

