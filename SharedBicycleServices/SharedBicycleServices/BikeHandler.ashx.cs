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
                SqlConnection con = new SqlConnection(Config.strCon);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                if (context.Request.HttpMethod.ToUpper() == "GET")
                {
                    String type = context.Request.QueryString["Type"];
                    if (type == "bike")
                    {
                        String subType = context.Request.QueryString["SubType"];
                        if (subType == "position")
                        {
                            cmd.CommandText = "select BikeID,tblBike.ModelID,tblBike.StateID,BikeLongitude,BikeLatitude,ModelName,StateName from tblBike,tblState,tblModel where tblBike.StateID=tblState.StateID and tblBike.ModelID=tblModel.ModelID and tblBike.StateID=1";
                        }
                        else if (subType == "info")
                        {
                            String pageNum = context.Request.QueryString["PageNum"];
                            int end = int.Parse(pageNum) * 10;
                            int start = (int.Parse(pageNum) - 1) * 10 + 1;
                            String strSql = "select BikeID,a.ModelID,a.StateID,BikeLongitude,BikeLatitude,ModelName,StateName from (select row_number()over(order by BikeID)rownumber,* from tblBike)a,tblState,tblModel where a.StateID=tblState.StateID and a.ModelID=tblModel.ModelID and rownumber between " + start + " and " + end;
                            String bikeID = context.Request.QueryString["BikeID"];
                            if (!String.IsNullOrEmpty(bikeID))
                            {
                                strSql += " and BikeID like '%"+bikeID+"%'";
                            }
                            String modelID = context.Request.QueryString["ModelID"];
                            if (!String.IsNullOrEmpty(modelID))
                            {
                                strSql += " and a.ModelID = '" + modelID + "'";
                            }
                            String stateID = context.Request.QueryString["StateID"];
                            if (!String.IsNullOrEmpty(stateID))
                            {
                                strSql += " and a.StateID = '" + stateID + "'";
                            }
                            cmd.CommandText = strSql;
                        }
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
                        dr.Close();
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
                        dr.Close();
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
                        dr.Close();
                    }
                    result.status = true;
                }
                if (context.Request.HttpMethod.ToUpper() == "PUT")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Bike bike = JsonConvert.DeserializeObject<Bike>(data);
                    cmd.CommandText = "update tblBike set ModelID='"+bike.ModelID+"',StateID='"+bike.StateID+"',BikeLongitude='"+bike.BikeLongitude+"',BikeLatitude='"+bike.BikeLatitude+"' where BikeID='"+bike.BikeID+"'";
                    cmd.ExecuteNonQuery();
                    result.status = true;
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Bike bike = JsonConvert.DeserializeObject<Bike>(data);
                    cmd.CommandText = "select * from tblBike where BikeID='"+bike.BikeID+"'";
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        result.message = "该车辆已在库中";
                        dr.Close();
                        context.Response.Write(JsonConvert.SerializeObject(result));
                        con.Close();
                        return;
                    }
                    dr.Close();
                    cmd.CommandText = "insert into tblBike(BikeID,ModelID,StateID,BikeLongitude,BikeLatitude) values(@BikeID,@ModelID,@StateID,@BikeLongitude,@BikeLatitude)";
                    cmd.Parameters.AddWithValue("@BikeID", bike.BikeID);
                    cmd.Parameters.AddWithValue("@ModelID", bike.ModelID);
                    cmd.Parameters.AddWithValue("@StateID", bike.StateID);
                    cmd.Parameters.AddWithValue("@BikeLongitude", bike.BikeLongitude);
                    cmd.Parameters.AddWithValue("@BikeLatitude", bike.BikeLatitude);
                    cmd.ExecuteNonQuery();
                    result.status = true;
                }
                if (context.Request.HttpMethod.ToUpper() == "DELETE")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Bike bike = JsonConvert.DeserializeObject<Bike>(data);
                    cmd.CommandText = "delete from tblBike where BikeID='" + bike.BikeID + "'";
                    cmd.ExecuteNonQuery();
                    result.status = true;
                }
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