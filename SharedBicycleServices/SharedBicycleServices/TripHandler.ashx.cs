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
    /// TripHandler 的摘要说明
    /// </summary>
    public class TripHandler : IHttpHandler
    {
        class Trip
        {
            public String TripID { get; set; }
            public String UserID { get; set; }
            public String BikeID { get; set; }
            public String StartTime { get; set; }
            public String EndTime { get; set; }
            public String Consume { get; set; }
            public String Position { get; set; }
            public String State { get; set; }
        }

        class Bike
        {
            public String BikeID { get; set; }
            public String ModelID { get; set; }
            public String StateID { get; set; }
            public String BikeLongitude { get; set; }
            public String BikeLatitude { get; set; }
        }

        class TripState
        {
            //state三种状态：unfinish/unpay/finish
            public String state { get; set; }
            public Trip trip { get; set; }
        }

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
                    String userID = context.Request.QueryString["UserID"];
                    cmd.CommandText = "select * from tblTrip where tblTrip.UserID = '" + userID + "' order by TripID DESC";
                    SqlDataReader dr = cmd.ExecuteReader();
                    TripState tripState = new TripState();
                    tripState.state = "finish";
                    if (dr.Read())
                    {
                        tripState.trip.TripID = dr["TripID"].ToString();
                        tripState.trip.UserID = dr["UserID"].ToString();
                        tripState.trip.BikeID = dr["BikeID"].ToString();
                        tripState.trip.StartTime = dr["StartTime"].ToString();
                        tripState.trip.EndTime = dr["EndTime"].ToString();
                        tripState.trip.Consume = dr["Consume"].ToString();
                        tripState.trip.Position = dr["Position"].ToString();
                        tripState.trip.State = dr["State"].ToString();
                        tripState.state = tripState.trip.State;
                    }
                    context.Response.Write(JsonConvert.SerializeObject(tripState));
                    dr.Close();
                }
                if (context.Request.HttpMethod.ToUpper() == "POST")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Trip trip = JsonConvert.DeserializeObject<Trip>(data);
                    cmd.CommandText = "insert into tblTrip(UserID,BikeID,StartTime,State) values(@UserID,@BikeID,@StartTime,@State)";
                    cmd.Parameters.AddWithValue("@UserID", trip.UserID);
                    cmd.Parameters.AddWithValue("@BikeID", trip.BikeID);
                    cmd.Parameters.AddWithValue("@StartTime", trip.StartTime);
                    cmd.Parameters.AddWithValue("@State", "unfinish");
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "select * from tblTrip where tblTrip.UserID = '" + trip.UserID + "' order by TripID DESC";
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        trip.TripID = dr["TripID"].ToString();
                    }
                    context.Response.Write(JsonConvert.SerializeObject(trip));
                    dr.Close();
                }
                if (context.Request.HttpMethod.ToUpper() == "PUT")
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    String data = sr.ReadToEnd();
                    Trip trip = JsonConvert.DeserializeObject<Trip>(data);
                    cmd.CommandText = "update into tblTrip(UserID,BikeID,StartTime,State) values(@UserID,@BikeID,@StartTime,@State)";
                    cmd.Parameters.AddWithValue("@UserID", trip.UserID);
                    cmd.Parameters.AddWithValue("@BikeID", trip.BikeID);
                    cmd.Parameters.AddWithValue("@StartTime", trip.StartTime);
                    cmd.Parameters.AddWithValue("@State", "unfinish");
                    cmd.ExecuteNonQuery();
                    result.status = true;
                    context.Response.Write(JsonConvert.SerializeObject(trip));
                }
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