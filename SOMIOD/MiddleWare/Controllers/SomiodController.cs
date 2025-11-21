using Newtonsoft.Json.Linq;
using MiddleWare.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MiddleWare.Controllers
{
    
    public class SomiodController : ApiController
    {
        string connectionString = Properties.Settings.Default.ConnStr;

        // GET: api/Products
        /// <summary>
        /// Gets all products from database
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Product> Get()
        {
            List<Product> lista = new List<Product>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM Prods", conn);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Product prod = new Product();
                        prod.Id = (int)reader["Id"];
                        prod.Name = (string)reader["Name"];
                        prod.Category = (string)reader["Category"];
                        prod.Price = (decimal)reader["Price"];
                        lista.Add(prod);
                    }
                }
            }
            return lista;
        }

        // GET: api/Products/5
        public IHttpActionResult Get(int id)
        {
            Product prod = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM Prods WHERE Id = @idProd", conn);
                command.Parameters.AddWithValue("@idProd", id);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        prod = new Product();
                        prod.Id = (int)reader["Id"];
                        prod.Name = (string)reader["Name"];
                        prod.Category = (string)reader["Category"];
                        prod.Price = (decimal)reader["Price"];
                    }
                }
            }
            if (prod == null)
            {
                return NotFound();
            }
            else
                return Ok(prod);
        }

        // POST: api/Products
        public IHttpActionResult Post([FromBody] Product value)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO Prods VALUES (@name, @cat, @price)";
                    SqlCommand command = new SqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@name", value.Name);
                    command.Parameters.AddWithValue("@cat", value.Category);
                    command.Parameters.AddWithValue("@price", value.Price);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                        return Ok();
                    else
                        return BadRequest();

                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // PUT: api/Products/5
        public IHttpActionResult Put(int id, [FromBody]Product value)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE Prods SET Name=@name, Category=@cat, Price=@price WHERE Id = @id";
                    SqlCommand command = new SqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@name", value.Name);
                    command.Parameters.AddWithValue("@cat", value.Category);
                    command.Parameters.AddWithValue("@price", value.Price);
                    command.Parameters.AddWithValue("@id", id);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                        return Ok();
                    else
                        return BadRequest();
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // DELETE: api/Products/5
        public IHttpActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE Prods WHERE Id = @id";
                    SqlCommand command = new SqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@id", id);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                        return Ok();
                    else
                        return BadRequest();
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }
    }
}
