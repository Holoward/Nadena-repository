using Domain.Common;

namespace Domain.Entities;

public class Client : AuditableBaseEntity
{
    private int _edad;
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Telefono { get; set; }
    public string Email { get; set; }
    public string Direction { get; set; }

    public int Edad
    {
        get
        {
            //if the _edad if minus or equals 0
            if (this._edad<=0)
            {
                // returning the age of the client base on the birthday
                this._edad = new DateTime(DateTime.Now.Subtract(this.FechaNacimiento).Ticks).Year - 1;
            }

            return this._edad;
        }
    }
}