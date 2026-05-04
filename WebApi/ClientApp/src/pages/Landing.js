import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const Landing = () => {
  const { auth } = useAuth();

  const s = {
    page: { minHeight: '100vh', background: '#0a1628', color: '#fff', fontFamily: 'sans-serif', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: 32 },
    logo: { fontSize: 32, fontWeight: 700, color: '#00D1FF', marginBottom: 16 },
    tagline: { fontSize: 18, color: '#a0aec0', marginBottom: 48, textAlign: 'center', maxWidth: 480 },
    row: { display: 'flex', gap: 16, flexWrap: 'wrap', justifyContent: 'center' },
    primary: { padding: '14px 36px', background: '#00D1FF', color: '#0a1628', border: 'none', borderRadius: 8, fontSize: 16, fontWeight: 700, cursor: 'pointer', textDecoration: 'none' },
    secondary: { padding: '12px 34px', background: 'transparent', color: '#fff', border: '2px solid rgba(255,255,255,0.3)', borderRadius: 8, fontSize: 16, fontWeight: 600, cursor: 'pointer', textDecoration: 'none' }
  };

  return (
    <div style={s.page}>
      <div style={s.logo}>NADENA</div>
      <p style={s.tagline}>Consent-based behavioral data platform. Contributors get paid. Researchers get data.</p>
      <div style={s.row}>
        <Link to="/register" style={s.primary}>Contribute Data</Link>
        <Link to="/marketplace" style={s.secondary}>Browse Datasets</Link>
        <Link to="/login" style={s.secondary}>Log In</Link>
      </div>
    </div>
  );
};

export default Landing;