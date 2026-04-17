import React from 'react';
import { Collapse, Container, Navbar, NavbarBrand, NavbarToggler, NavItem, NavLink } from 'reactstrap';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './NavMenu.css';

const NavMenu = () => {
  const { auth, logout } = useAuth();
  const [collapsed, setCollapsed] = React.useState(true);

  const toggleNavbar = () => {
    setCollapsed(!collapsed);
  };

  const handleLogout = () => {
    logout();
  };

  const renderNavLinks = () => {
    if (auth.isAuthenticated) {
      // Show dashboard link based on role
      switch (auth.role) {
        case 'Data Contributor':
          return (
            <NavItem>
              <NavLink tag={Link} className="text-white" to="/contributor/dashboard">Dashboard</NavLink>
            </NavItem>
          );
        case 'Data Client':
          return (
            <NavItem>
              <NavLink tag={Link} className="text-white" to="/client/dashboard">Dashboard</NavLink>
            </NavItem>
          );
        case 'Admin':
          return (
            <NavItem>
              <NavLink tag={Link} className="text-white" to="/admin/dashboard">Dashboard</NavLink>
            </NavItem>
          );
        default:
          return null;
      }
    }
    return null;
  };

  return (
    <header>
      <Navbar className="navbar-expand-sm navbar-toggleable-sm border-bottom box-shadow mb-3" style={{ backgroundColor: '#1a2f4a' }} dark>
        <Container>
          <NavbarBrand tag={Link} to="/" style={{ color: '#fff', fontWeight: 'bold' }}>NADENA</NavbarBrand>
          <NavbarToggler onClick={toggleNavbar} className="mr-2" />
          <Collapse className="d-sm-inline-flex flex-sm-row-reverse" isOpen={!collapsed} navbar>
            <ul className="navbar-nav flex-grow">
              <NavItem>
                <NavLink tag={Link} className="text-white" to="/">Home</NavLink>
              </NavItem>
              {!auth.isAuthenticated && (
                <>
                  <NavItem>
                    <NavLink tag={Link} className="text-white" to="/login">Login</NavLink>
                  </NavItem>
                  <NavItem>
                    <NavLink tag={Link} className="text-white" to="/register">Register</NavLink>
                  </NavItem>
                </>
              )}
              {renderNavLinks()}
              {auth.isAuthenticated && (
                <NavItem>
                  <NavLink tag={Link} className="text-white" to="/account/settings">Account</NavLink>
                </NavItem>
              )}
              {auth.isAuthenticated && (
                <NavItem>
                  <NavLink tag={Link} className="text-white" to="/" onClick={handleLogout} style={{ cursor: 'pointer' }}>Logout</NavLink>
                </NavItem>
              )}
            </ul>
          </Collapse>
        </Container>
      </Navbar>
    </header>
  );
};

export default NavMenu;
