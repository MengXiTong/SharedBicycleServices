using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.IO;
using Newtonsoft.Json;

namespace SharedBicycleServices
{
    /// <summary>
    /// BikeHandler 的摘要说明
    /// </summary>

    class Bike
    {
        public String BikeID { get; set; }
        public String ModelID { get; set; }
        public String StateID { get; set; }
        public String BikeLongitude { get; set; }
        public String BikeLatitude { get; set; }
        public String ModelName { get; set; }
        public String StateName { get; set; }
    }

    class Model
    {
        public String ModelID { get; set; }
        public String ModelName { get; set; }
    }

    class State
    {
        public String StateID { get; set; }
        public String StateName { get; set; }
    }

    public class BikeHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            Result result = new Result();
            result.status = false;
            try
            {
                SqlConnection con = new SqlConnection("server=localhost;database=SharedBicycle;user id=sa;password=123456");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                if (context.Request.HttpMethod.ToUpper() == "GET")
                {
                    String type = context.Request.QueryString["Type"];
                    if (type == "bike")
                    {
                        cmd.CommandText = "select BikeID,tblBike.ModelID,tblBike.StateID,BikeLongitude,BikeLatitude,ModelName,StateName from tblBike,tblState,tblModel where tblBike.StateID=tblState.StateID and tblBike.ModelID=tblModel.ModelID";
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Bike> bikeList = new List<Bike>();
                        while (dr.Read())
                        {
                            Bike bike = new Bike();
                            bike.BikeID = dr["BikeID"].ToString();
                            bike.ModelID = dr["ModelID"].ToString();
                            bike.StateID = dr["StateID"].ToString();
                            bike.BikeLongitude = dr["BikeLongitude"].ToString();
                            bike.BikeLatitude = dr["BikeLatitude"].ToString();
                            bike.ModelName = dr["ModelName"].ToString();
                            bike.StateName = dr["StateName"].ToString();
                            bikeList.Add(bike);
                        }
                        result.bikeList = bikeList;
                    }
                    if (type == "model")
                    {
                        cmd.CommandText = "select * from tblModel";
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Model> modelList = new List<Model>();
                        while (dr.Read())
                        {
                            Model model = new Model();
                            model.ModelID = dr["ModelID"].ToString();
                            model.ModelName = dr["ModelName"].ToString();
                            modelList.Add(model);
                        }
                        result.modelList = modelList;
                    }
                    if (type == "state")
                    {
                        cmd.CommandText = "select * from tblState";
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<State> stateList = new List<State>();
                        while (dr.Read())
                        {
                            State state = new State();
                            state.StateID = dr["StateID"].ToString();
                            state.StateName = dr["StateName"].ToString();
                            stateList.Add(state);
                        }
                        result.stateList = stateList;
                    }
                }
                result.status = true;
                context.Response.Write(JsonConvert.SerializeObject(result));
                con.Close();
            }
            catch (Exception error)
            {
                result.message = error.ToString();
                context.Response.Write(JsonConvert.SerializeObject(result));
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}