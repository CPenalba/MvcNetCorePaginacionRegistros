using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using MvcNetCorePaginacionRegistros.Data;
using MvcNetCorePaginacionRegistros.Models;
using System.Data;

#region
//CREATE VIEW V_DEPARTAMENTOS_INDIVIDUAL
//AS
//SELECT CAST(
//ROW_NUMBER() OVER (ORDER BY DEPT_NO)AS INT) AS POSICION, DEPT_NO, DNOMBRE, LOC FROM DEPT
//GO


//CREATE PROCEDURE SP_GRUPO_DEPARTAMENTOS
//(@posicion int)
//AS
//SELECT DEPT_NO, DNOMBRE, LOC FROM V_DEPARTAMENTOS_INDIVIDUAL
//WHERE POSICION >= @posicion AND POSICION < (@posicion + 2)
//GO

//CREATE VIEW V_GRUPO_EMPLEADOS
//AS
//SELECT CAST(
//ROW_NUMBER() OVER (ORDER BY APELLIDO)AS INT) AS POSICION, EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO FROM EMP
//GO

//CREATE PROCEDURE SP_GRUPO_EMPLEADOS
//(@posicion int)
//AS
//SELECT EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO FROM V_GRUPO_EMPLEADOS
//WHERE POSICION >= @posicion AND POSICION < (@posicion + 3)
//GO

//CREATE PROCEDURE SP_GRUPO_EMPLEADOS_OFICIO
//(@posicion int, @oficio nvarchar(50))
//AS
//SELECT EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO FROM
//(SELECT ROW_NUMBER() OVER (ORDER BY APELLIDO) AS POSICION, EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO FROM EMP
//WHERE OFICIO = @oficio) QUERY WHERE POSICION >= @posicion AND POSICION < (@posicion +2)
//GO

//CREATE PROCEDURE SP_GRUPO_EMPLEADOS_OFICIO_OUT
//(@posicion INT,
//@oficio NVARCHAR(50),
//@registros INT OUT)
//AS
//	SELECT @registros = COUNT(EMP_NO) FROM EMP
//	WHERE OFICIO = @oficio
//	SELECT EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO FROM (
//		SELECT CAST(
//		ROW_NUMBER() OVER (ORDER BY APELLIDO) AS INT) AS POSICION
//		, EMP_NO, APELLIDO, OFICIO, SALARIO, DEPT_NO
//		FROM EMP WHERE OFICIO = @oficio) QUERY
//		WHERE POSICION >= @posicion AND 
//		POSICION < (@posicion +2)
//GO
#endregion

namespace MvcNetCorePaginacionRegistros.Repositories
{
    public class RepositoryHospital
    {
        private HospitalContext context;

        public RepositoryHospital(HospitalContext context)
        {
            this.context = context;
        }

        public async Task<List<Departamento>> GetDepartamentosAsync()
        {
            return await this.context.Departamentos.ToListAsync();
        }

        public async Task<List<Empleado>> GetEmpleadosDepartamentoAsync(int idDepartamento)
        {
            var empleados = this.context.Empleados.Where(x => x.IdDepartamento == idDepartamento);
            if (empleados.Count() == 0)
            {
                return null;
            }
            else
            {
                return await empleados.ToListAsync();
            }
        }

        public async Task<int> GetNumeroRegistrosVistaDepartamentosAsync()
        {
            return await this.context.VistaDepartamentos.CountAsync();
        }

        public async Task<VistaDepartamento> GetVistaDepartamentoAsync(int posicion)
        {
            VistaDepartamento departamento = await this.context.VistaDepartamentos.Where(z => z.Posicion == posicion).FirstOrDefaultAsync();
            return departamento;
        }

        public async Task<List<VistaDepartamento>> GetGrupoVistaDepartamentoAsync(int posicion)
        {
            //SELECT * FROM V_DEPARTAMENTOS_INDIVIDUAL
            //WHERE POSICION >= 1 AMD POSICION < (1 + 2)
            var consulta = from datos in this.context.VistaDepartamentos
                           where datos.Posicion >= posicion
                           && datos.Posicion < (posicion + 2)
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<List<Departamento>> GetGrupoDepartamentosAsync(int posicion)
        {
            string sql = "SP_GRUPO_DEPARTAMENTOS @posicion";
            SqlParameter pamPosicion = new SqlParameter("@posicion", posicion);
            var consulta = this.context.Departamentos.FromSqlRaw(sql, pamPosicion);
            return await consulta.ToListAsync();
        }
        public async Task<int> GetEmpleadosCountAsync()
        {
            return await this.context.Empleados.CountAsync();
        }

        public async Task<List<Empleado>> GetGrupoEmpleadosAsync(int posicion)
        {
            string sql = "SP_GRUPO_EMPLEADOS @posicion";
            SqlParameter pamPosicion = new SqlParameter("@posicion", posicion);
            var consulta = this.context.Empleados.FromSqlRaw(sql, pamPosicion);
            return await consulta.ToListAsync();
        }

        public async Task<int> GetEmpleadosOficioCountAsync(string oficio)
        {
            return await this.context.Empleados
                .Where(z => z.Oficio == oficio).CountAsync();
        }

        public async Task<List<Empleado>> GetEmpleadosOficioAsync(int posicion, string oficio)
        {
            string sql = "SP_GRUPO_EMPLEADOS_OFICIO @posicion, @oficio";
            SqlParameter pamPosicion = new SqlParameter("@posicion", posicion);
            SqlParameter pamOficio = new SqlParameter("@oficio", oficio);
            var consulta = this.context.Empleados.FromSqlRaw(sql, pamPosicion, pamOficio);
            return await consulta.ToListAsync();
        }

        public async Task<ModelEmpleadosOficio> GetEmpleadosOficioOutAsync(int posicion, string oficio)
        {
            string sql = "SP_GRUPO_EMPLEADOS_OFICIO_OUT @posicion, @oficio, @registros out";
            SqlParameter pamPosicion = new SqlParameter("@posicion", posicion);
            SqlParameter pamOficio = new SqlParameter("@oficio", oficio);
            SqlParameter pamRegistros = new SqlParameter("@registros", 0);
            pamRegistros.Direction = ParameterDirection.Output;
            var consulta = this.context.Empleados.FromSqlRaw
                (sql, pamPosicion, pamOficio, pamRegistros);
            //PRIMERO DEBEMOS EJECUTAR LA CONSULTA PARA PODER RECUPERAR LOS PARAMETROS DE SALIDA
            List<Empleado> empleados = await consulta.ToListAsync();
            int registros = int.Parse(pamRegistros.Value.ToString());
            return new ModelEmpleadosOficio
            {
                NumeroRegistros = registros,
                Empleados = empleados
            };
        }
    }
}
