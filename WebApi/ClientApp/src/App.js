import React, { useEffect } from 'react';
import { Route, Switch, useHistory, useLocation } from 'react-router';
import { AuthProvider, useAuth } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import NavMenu from './components/NavMenu';
import Landing from './pages/Landing';
import Login from './pages/Login';
import Register from './pages/Register';
import VolunteerDashboard from './pages/VolunteerDashboard';
import BuyerDashboard from './pages/BuyerDashboard';
import AdminDashboard from './pages/AdminDashboard';
import DataSources from './pages/DataSources';
import DataInterests from './pages/DataInterests';
import OnboardingTerms from './pages/OnboardingTerms';
import OnboardingConsent from './pages/OnboardingConsent';
import AccountSettings from './pages/AccountSettings';
import ResetPassword from './pages/ResetPassword';
import VerifyEmail from './pages/VerifyEmail';
import TakeoutUpload from './pages/TakeoutUpload';
import ContributorStats from './pages/ContributorStats';
import Marketplace from './pages/Marketplace';

import './custom.css'

function AuthRouter() {
  const { auth } = useAuth();
  const history = useHistory();
  const location = useLocation();

  useEffect(() => {
    if (auth.isAuthenticated && location.pathname === '/') {
      switch (auth.role) {
        case 'Data Contributor':
          history.replace('/contributor/dashboard');
          break;
        case 'Data Client':
          history.replace('/client/dashboard');
          break;
        case 'Admin':
          history.replace('/admin/dashboard');
          break;
        default:
          break;
      }
    }
  }, [auth.isAuthenticated, auth.role, history, location.pathname]);

  return null;
}

function App() {
  return (
    <AuthProvider>
      <AuthRouter />
      <NavMenu />
      <Switch>
        {/* Public Routes */}
        <Route exact path="/" component={Landing} />
        <Route path="/login" component={Login} />
        <Route path="/register" component={Register} />
        <Route path="/reset-password" component={ResetPassword} />
        <Route path="/verify-email" component={VerifyEmail} />
        <Route path="/marketplace" component={Marketplace} />

        {/* Protected Routes */}
        <ProtectedRoute path="/onboarding/terms" component={OnboardingTerms} allowedRole="Data Contributor" />
        <ProtectedRoute path="/onboarding/consent" component={OnboardingConsent} allowedRole="Data Contributor" />

        <ProtectedRoute path="/contributor/dashboard" component={VolunteerDashboard} allowedRole="Data Contributor" requireOnboarding />
        <ProtectedRoute path="/client/dashboard" component={BuyerDashboard} allowedRole="Data Client" />
        <ProtectedRoute path="/admin/dashboard" component={AdminDashboard} allowedRole="Admin" />
        <ProtectedRoute path="/account/settings" component={AccountSettings} />
        <ProtectedRoute path="/setup/data-sources" component={DataSources} allowedRole="Data Contributor" requireOnboarding />
        <ProtectedRoute path="/setup/data-interests" component={DataInterests} allowedRole="Data Client" />
        
        {/* Takeout Upload & Stats */}
        <ProtectedRoute path="/upload" component={TakeoutUpload} allowedRole="Data Contributor" />
        <ProtectedRoute path="/stats" component={ContributorStats} allowedRole="Data Contributor" />

        {/* Catch all - redirect to home */}
        <Route path="*" component={Landing} />
      </Switch>
    </AuthProvider>
  );
}

export default App;