import React, { useEffect, useState } from 'react';
import { Redirect, Route } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const ProtectedRoute = ({ component: Component, allowedRole, requireOnboarding = false, ...rest }) => {
    const { auth } = useAuth();
    const [onboardingChecked, setOnboardingChecked] = useState(!requireOnboarding);
    const [onboardingNextStep, setOnboardingNextStep] = useState(null);

    useEffect(() => {
        const check = async () => {
            if (!requireOnboarding) return;
            if (!auth.isAuthenticated) return;
            if (auth.role !== 'Data Contributor') {
                setOnboardingChecked(true);
                return;
            }

            try {
                const res = await fetch('/api/v1/Onboarding/status', {
                    headers: {
                        'Authorization': `Bearer ${auth.token}`,
                        'Content-Type': 'application/json'
                    }
                });
                if (res.ok) {
                    const data = await res.json();
                    setOnboardingNextStep(data.nextStep || null);
                }
            } catch (e) {
                // If the status call fails, don't hard-block navigation.
                setOnboardingNextStep(null);
            } finally {
                setOnboardingChecked(true);
            }
        };
        check();
    }, [requireOnboarding, auth.isAuthenticated, auth.role, auth.token]);

    return (
        <Route
            {...rest}
            render={(props) => {
                // Check if user is authenticated
                if (!auth.isAuthenticated) {
                    return <Redirect to="/login" />;
                }

                // Check if user has the required role
                if (allowedRole && auth.role !== allowedRole) {
                    return <Redirect to="/" />;
                }

                // Enforce Data Contributor onboarding before accessing protected pages
                if (requireOnboarding && auth.role === 'Data Contributor') {
                    if (!onboardingChecked) {
                        return (
                            <div style={{ padding: 40, textAlign: 'center' }}>
                                <div className="spinner-border text-primary" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                                <p className="mt-3">Loading...</p>
                            </div>
                        );
                    }

                    if (onboardingNextStep) {
                        return <Redirect to={onboardingNextStep} />;
                    }
                }

                // Render the component if authenticated and authorized
                return <Component {...props} />;
            }}
        />
    );
};

export default ProtectedRoute;
