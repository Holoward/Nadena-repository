import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { Link } from 'react-router-dom';

export function ClientesList() {
    const [clientes, setClientes] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchClientes();
    }, []);

    const fetchClientes = async () => {
        try {
            const response = await axios.get('/api/v1/Clientes');
            setClientes(response.data.data || []);
            setLoading(false);
        } catch (error) {
            console.error("Error fetching clientes", error);
            setLoading(false);
        }
    };

    const handleDelete = async (id) => {
        if (window.confirm("Are you sure you want to delete this client?")) {
            try {
                await axios.delete(`/api/v1/Clientes/${id}`);
                fetchClientes(); // Refresh list
            } catch (error) {
                console.error("Error deleting client", error);
            }
        }
    };

    if (loading) {
        return <p><em>Loading...</em></p>;
    }

    return (
        <div>
            <h1 id="tableLabel">Clientes</h1>
            <p>This component demonstrates fetching data from the new ClientesController.</p>
            <Link to="/cliente-form" className="btn btn-primary mb-3">Add New Client</Link>
            
            <table className='table table-striped' aria-labelledby="tableLabel">
                <thead>
                    <tr>
                        <th>Nombre</th>
                        <th>Apellido</th>
                        <th>Edad</th>
                        <th>Email</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {clientes.map(cliente =>
                        <tr key={cliente.id}>
                            <td>{cliente.nombre}</td>
                            <td>{cliente.apellido}</td>
                            <td>{cliente.edad}</td>
                            <td>{cliente.email}</td>
                            <td>
                                <Link to={`/cliente-form/${cliente.id}`} className="btn btn-sm btn-warning me-2">Edit</Link>
                                <button onClick={() => handleDelete(cliente.id)} className="btn btn-sm btn-danger">Delete</button>
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
}
