using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;


public class MovePlayer : MonoBehaviour {

    //public float movementSpeed = 2;
    //Earth radius in KM:
    const float EARTH_RADIUS = 6371;
    const float LOC_DELTA_TIME_MAX = 2; //[sec] Max location update tiemout
    const float LOC_DELTA_TIME_GPS_MAX = 30;
    const float LOC_DELTA_DISTANCE = 4;//delta distance to check nearby attractions

    const bool IsSimMode = true; //Defines simulation mode flag
    public float distance = 0;
    public float curr_deltaDistance = 0;

    public float latitude = 0, longitude = 0;

    public bool fToDuplicate = true;//Use to clone only once

    public float delta_loc_time_changed = 0;//total delta time between location changes
    public bool LocationChanged_f = false;//indication that location has changed

    //public AudioSource audio; //Audio indication of location change

    public float approx_velocity = 0;//approximated velocity
    public float approx_bearing = 0;//approximated bearing

    //Sample running route from rosh a hain:
    public int route_index = 0;
    public int sample_rate = 10;
    public int MAX_WP = 43;//Max waypoints

    //Holds current closest attraction:
    public Attraction Closest_attraction;

    //Simulated waypoints: (from home to safta):
    public double[] route_lat = {
                                     32.09631,
                                    32.09671,
                                    32.09795,
                                    32.09879,
                                    32.099,
                                    32.0981,
                                    32.0972,
                                    32.09657,
                                    32.0969,
                                    32.09711,
                                    32.09671,
                                    32.09694,
                                    32.0978,
                                    32.09957,
                                    32.1014,
                                    32.10424,
                                    32.1054,
                                    32.10609,
                                    32.10679,
                                    32.10755,
                                    32.10809,
                                    32.11014,
                                    32.11119,
                                    32.11301,
                                    32.11461,
                                    32.11621,
                                    32.11635,
                                    32.11839,
                                    32.12101,
                                    32.12217,
                                    32.12144,
                                    32.11781,
                                    32.1149,
                                    32.1117,
                                    32.10908,
                                    32.10385,
                                    32.10134,
                                    32.09901,
                                    32.09771,
                                    32.09791,
                                    32.09791,
                                    32.09559,
                                    32.0949,
                                    32.09351
    };

    public double[] route_lon =
    {
                        34.97628,
                        34.97692,
                        34.97697,
                        34.97624,
                        34.97486,
                        34.97486,
                        34.9751,
                        34.97448,
                        34.97328,
                        34.9719,
                        34.97124,
                        34.97056,
                        34.97096,
                        34.97038,
                        34.96862,
                        34.96596,
                        34.96368,
                        34.96171,
                        34.96136,
                        34.9614,
                        34.96148,
                        34.95833,
                        34.95612,
                        34.95144,
                        34.94578,
                        34.94011,
                        34.93153,
                        34.91866,
                        34.90767,
                        34.89994,
                        34.88793,
                        34.87934,
                        34.87608,
                        34.87076,
                        34.86664,
                        34.86853,
                        34.8699,
                        34.87231,
                        34.87814,
                        34.88057,
                        34.88146,
                        34.88225,
                        34.88,
                        34.88033

    };




    //JSON response from google places webservice:
    [System.Serializable]
    public class Location
    {
        public double lat;
        public double lng;
    }
    [System.Serializable]
    public class Northeast
    {
        public double lat;
        public double lng;
    }
    [System.Serializable]
    public class Southwest
    {
        public double lat;
        public double lng;
    }
    [System.Serializable]
    public class Viewport
    {
        public Northeast northeast;
        public Southwest southwest;
    }
    [System.Serializable]
    public class Geometry
    {
        public Location location;
        public Viewport viewport;
    }
    [System.Serializable]
    public class OpeningHours
    {
        public bool open_now;
        public List<object> weekday_text;
    }
    [System.Serializable]
    public class Photo
    {
        public int height;
        public List<string> html_attributions;
        public string photo_reference;
        public int width;
    }
    [System.Serializable]
    public class Result
    {
        public Geometry geometry;
        public string icon;
        public string id;
        public string name;
        public OpeningHours opening_hours;
        public List<Photo> photos;
        public string place_id;
        public double rating;
        public string reference;
        public string scope;
        public List<string> types;
        public string vicinity;
    }

    [System.Serializable]
    public class PlacesApiQueryResponse
    {
        public List<object> html_attributions;
        public string next_page_token;
        public List<Result> results;
        public string status;
    }

    //Using this class for display the attraction in 3D:
    public class Attraction
    {
        public Location location;
        public double rating;
        public bool open_now;
        public string name;
        public string vicinity;
        public bool valid;
        public string status;
    }

    public class Direction_
    {
        public float distance;
        public float bearing;
        public float x;
        public float y;
    }

    public Location CurrentLoc;//Storing curring location
    public Direction_ DirectionToTarget;//storing the closest attraction

    //Get closest attraction: (fills the closest public "Closest_attraction" structure)
    IEnumerator GetClosestAttraction(float curr_lat, float curr_lon)
    //void GetClosestAttraction(float curr_lat, float curr_lon, ref Attraction Closest_attraction)
    {

        //curr_lat = (float)32.0958;
        //curr_lon = (float)34.9522;
        //Try to receive json from googlemap service:
        string url = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?key=AIzaSyCRh4hrKB-avvHS4kN6lxR5crpJVEuGdP8&location=" +
            curr_lat.ToString() + "," + curr_lon.ToString() + "&rankby=distance&keyword=Ice+Cream";

        //string url = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?key=AIzaSyCRh4hrKB-avvHS4kN6lxR5crpJVEuGdP8&location=32.0958,34.9522&rankby=distance&keyword=Ice+Cream";
        //string url = string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0},{1}&radius=500&type=bar&key=AIzaSyCRh4hrKB-avvHS4kN6lxR5crpJVEuGdP8", latitude, longitude);
        WWW json = new WWW(url);

        //Wait till the hole json data is downloaded:
        while (!json.isDone)
        {
            System.Threading.Thread.Sleep(20);
        }
        string jsonString = json.text;

        Debug.Log("Got Json: " + json.text);

        // PlacesApiQueryResponse ClosestAttraction = JsonUtility.FromJson<PlacesApiQueryResponse>(json.text);
        PlacesApiQueryResponse ClosestAttraction = JsonUtility.FromJson<PlacesApiQueryResponse>(json.text);

        const int Wres = 1;
        //If no results, exit:
        Closest_attraction.status = ClosestAttraction.status;
        if (ClosestAttraction.status.ToString() == "ZERO_RESULTS")
            yield return null;

        if (ClosestAttraction.results[Wres] != null)
        {
            Closest_attraction.location = ClosestAttraction.results[Wres].geometry.location;
            Closest_attraction.rating = ClosestAttraction.results[Wres].rating;
            Closest_attraction.name = ClosestAttraction.results[Wres].name;
            Closest_attraction.open_now = ClosestAttraction.results[Wres].opening_hours.open_now;
            Closest_attraction.vicinity = ClosestAttraction.results[Wres].vicinity;
            Closest_attraction.valid = true;
        }
        yield return null;
    }




    // Initialization of the Player:
    void Start()
    {

        //Location of our home:
        //wpt lat = "32.09834" lon = "34.97702"

        //Location of Safta miriam:
        //< wpt lat = "32.09363" lon = "34.88043" >

        //IsSimMode = false;
        //Start locations services:
        Input.location.Start();

        //Set initial location:
        GetCoordinates(ref latitude, ref longitude, IsSimMode, ref route_index);

        delta_loc_time_changed = 0;// reset the  accumulative delta time
                                   //var result = GetClosestAttraction(latitude, longitude);

        //Initialize the current attraction:
        Closest_attraction = new Attraction();
        Closest_attraction.valid = false;
        DirectionToTarget = new Direction_();

        //Set the initial state of the camera:
        SetLocVRObj(0, 0, "Player1");
        SetLocVRObj(0, 0, "MainCamera");


        //Create home:
        Location home_loc = new Location();
        home_loc.lat = 32.09834; home_loc.lng = 34.97702;
        //GetDirection(ref home_loc, ref home_loc, ref DirectionToTarget);
        //StartCoroutine(CreateAttractionInVR(0, 0, "Home"));
        SetLocVRObj(0, 0, "Home");

        //Create Safta home:
        Location tmp_loc = new Location();
        tmp_loc.lat = 32.09363; tmp_loc.lng = 34.88043;
        GetDirection(ref home_loc, ref tmp_loc, ref DirectionToTarget);
        SetLocVRObj(DirectionToTarget.x, DirectionToTarget.y, "Safta");
        //StartCoroutine(CreateAttractionInVR(DirectionToTarget.x, DirectionToTarget.y, "Safta"));

        //Create Sagive coodinates:
        //wpt lat = "32.09881" lon = "34.97494" >
        tmp_loc.lat = 32.09881; tmp_loc.lng = 34.97494;
        GetDirection(ref home_loc, ref tmp_loc, ref DirectionToTarget);
        //StartCoroutine(CreateAttractionInVR(DirectionToTarget.x, DirectionToTarget.y, "Grass"));
        SetLocVRObj(DirectionToTarget.x, DirectionToTarget.y, "Grass");

        //Get closest attraction:
        //GetClosestAttraction(latitude, longitude, ref Closest_attraction);
        StartCoroutine(GetClosestAttraction(latitude, longitude));

        GetClosestAttraction(latitude, longitude);
        CurrentLoc.lat = latitude; CurrentLoc.lng = longitude;

        if (Closest_attraction.valid)
        {
            //Get direction to target:
            GetDirection(ref CurrentLoc, ref Closest_attraction.location, ref DirectionToTarget);
            StartCoroutine(CreateAttractionInVR(DirectionToTarget.x, DirectionToTarget.y, "Billboard110"));
            //CreateAttractionInVR(DirectionToTarget.x, DirectionToTarget.y);
        }
    }

    //Create attraction object in VR by x, and y coordinates
    IEnumerator CreateAttractionInVR(float x, float y,string VRname)
    {
        //Duplicate the road blocks:
        //RectTransform Grass_ = GetComponent<RectTransform>();
        GameObject Vobject = GameObject.Find(VRname);
        //BillBoard.transform.Translate(Vector3.forward * x);
        //BillBoard.transform.Translate(Vector3.right * y);
        Vobject.transform.Translate(Vector3.forward * x);
        Vobject.transform.Translate(Vector3.right * y);
        //Vobject.transform.position.Set(y, x, 61.0f);

        //BillBoard.transform.Translate(Vector3.up * (float)65.67);
        yield return null;
        /*
        GameObject clone;
        clone = Instantiate(BillBoard, transform.position, transform.rotation) as GameObject;
        clone.transform.Translate(Vector3.forward*y);
        clone.transform.Translate(Vector3.right * x);
        fToDuplicate = false;
        */

    }

    //Sets location of the objects in VR world by x, y relative to center of the world (my home)
    public void SetLocVRObj(float x, float y, string VRname)
    {
        GameObject Vobject = GameObject.Find(VRname);
        //Vobject.transform.position.Set(y, x, 61.0f);
        Vobject.transform.position = new Vector3(y, 61.0f, x);
    }

    //Function returns lattitude and lontitude, enables sim mode:
    void GetCoordinates(ref float lat, ref float lon, bool IsSim, ref int cord_index)
    {
        //If to use simulated location route:
        if (IsSim)
        {
            //If not at the last way point in route:
            if (cord_index < MAX_WP)
            {
                lat = (float)route_lat[cord_index];
                lon = (float)route_lon[cord_index];

                //Change to next WP if passed LOC_DELTA_TIME_MAX seconds:
                if (delta_loc_time_changed > LOC_DELTA_TIME_MAX)
                    cord_index = cord_index + 1;
                
            }
            else cord_index = 0;
        }
        else
        {
            lat = Input.location.lastData.latitude;
            lon = Input.location.lastData.longitude;
        }

    }


    //Check if GPS location changed:
    void CheckLocationChanged(float lat_new, float lat_last, float lon_new, float lon_last)
    {
        if ((Mathf.Abs(lat_new - lat_last) > 0) || Mathf.Abs(lon_new - lon_last) > 0)
        {
            LocationChanged_f = true;
            //audio.Play();
        }
        LocationChanged_f = ((Mathf.Abs(lat_new - lat_last) > 0) && Mathf.Abs(lon_new - lon_last) > 0);
    }

    //Check if GPS location changed:
    bool LocationChanged()
    {
        return LocationChanged_f;
    }

    //Get direction from pt. of origin to target : 
    //distance[km] and bearing[deg from north] from pt of origin to target location:
    void GetDirection(ref Location Location_source, 
        ref Location Location_target, ref Direction_ DirToTarget)
    {
       
        float lat2 = (float)Location_target.lat;
        float lat1 = (float)Location_source.lat;
        float lon2 = (float)Location_target.lng;
        float lon1 = (float)Location_source.lng;

        
        float R = EARTH_RADIUS; // metres
        float φ1 = lat1*Mathf.Deg2Rad;
        float φ2 = lat2*Mathf.Deg2Rad;
        float λ1 = lon1 * Mathf.Deg2Rad;
        float λ2 = lon2 * Mathf.Deg2Rad;

        float Δφ = (lat2 - lat1)*Mathf.Deg2Rad;
        float Δλ = (lon2 - lon1)*Mathf.Deg2Rad;

        float a = Mathf.Sin(Δφ / 2) * Mathf.Sin(Δφ / 2) +
                Mathf.Cos(φ1) * Mathf.Cos(φ2) *
                Mathf.Sin(Δλ / 2) * Mathf.Sin(Δλ / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        float d = R * c;
        DirToTarget.distance = d;//distance in km

        //Calculate the bearing:
        //where φ1, λ1 is the start point, φ2,λ2 the end point(Δλ is the difference in longitude)

        float y = Mathf.Sin(λ2 - λ1) * Mathf.Cos(φ2);
        float x = Mathf.Cos(φ1) * Mathf.Sin(φ2) -
                Mathf.Sin(φ1) * Mathf.Cos(φ2) * Mathf.Cos(λ2 - λ1);
        float brng = Mathf.Atan2(y, x)*Mathf.Rad2Deg;

        DirToTarget.bearing = brng;//bearing in [deg]
        DirToTarget.x = x * R; //[x in km]
        DirToTarget.y = y * R;//[y in km]

        
    }

    //Function calculates the distance in meters from current location (lat lon)
    float Haversine(ref float lastLatitude, ref float lastLontitude, ref float bearing, ref float x, ref float y)
    {

        float newLatitude = 0;
        float newLontitude = 0;
        x = 0;y = 0;

        //Get coordinates from GPS/given route:
        GetCoordinates(ref newLatitude, ref newLontitude, IsSimMode, ref route_index);
        //Check if location has changed:
        CheckLocationChanged(newLatitude, lastLatitude, newLontitude, lastLontitude);

        Location curr_loc = new Location();
        curr_loc.lat = lastLatitude;
        curr_loc.lng = lastLontitude;

        Location next_loc = new Location();
        next_loc.lat = newLatitude;
        next_loc.lng = newLontitude;

        Direction_ Direction = new Direction_();
        //Calculate the direction and distance:
        GetDirection(ref curr_loc,ref next_loc, ref Direction);
        x = Direction.x;
        y = Direction.y;

        if (LocationChanged())
        {
            //If location changed, updated the bearing:
            bearing = Direction.bearing;
            approx_bearing = bearing;
        }
        else //Assume the same bearing as before:
        {
            bearing = approx_bearing;
        }

        lastLatitude = newLatitude;
        lastLontitude = newLontitude;

        return Direction.distance;

        
    }

    
    
    
    //Calculate velocity [m/sec]:
    float CalcVelocity(float delta_distance)
    {
        //If location hasn't changed, assume movement and add the delta refresh time to total time:
        if (!LocationChanged())
        {
            delta_loc_time_changed += Time.deltaTime;
            //If didn't get loc update more than LOC_DELTA_TIME_GPS_MAX then stop
            if (delta_loc_time_changed > LOC_DELTA_TIME_GPS_MAX)
                approx_velocity = 0;
            return approx_velocity;
        }
        else
        {
            if (delta_loc_time_changed > 0)
            { 
                float vel_ = delta_distance / delta_loc_time_changed;
                //If  velocity is in the reasonable limits [0..1000]kph
                if ( vel_< (1000/3.6f))
                    approx_velocity = vel_;
                delta_loc_time_changed = 0;
            }
            return approx_velocity;
        }

        //return (float)lastDistance / lastTime * 3.6f;
    }


    //Generate smooth vr movement based on the velocity and bearing:
    void GenSmoothedMovement(float delta_distance, float bearing, float speed,float x, float y)
    {
        float forward_ = 0, right_ = 0;
        //float forward_ = ((float)deltaDistance * Mathf.Cos(bearing * Mathf.Deg2Rad)) / 100;
        if (IsSimMode)
        {
            //forward_ = speed * Time.deltaTime * Mathf.Cos(bearing * Mathf.Deg2Rad) / 100;
            forward_ = speed * Time.deltaTime * Mathf.Cos(bearing * Mathf.Deg2Rad)/400;
            right_ = speed * Time.deltaTime * Mathf.Sin(bearing * Mathf.Deg2Rad)/400;
        }
        else
        {
            forward_ = speed * Time.deltaTime * Mathf.Cos(bearing * Mathf.Deg2Rad) / 20;
            right_ = speed * Time.deltaTime * Mathf.Sin(bearing * Mathf.Deg2Rad) / 20;

        }

        if ((right_ != 0) & (forward_ != 0))
        {
            transform.Translate(Vector3.forward * forward_);
            transform.Translate(Vector3.right * right_);
            //transform.Translate(Vector3.right * y);
            //transform.Translate(Vector3.forward * x);
        }
    }


    // Update is called once per frame
    void Update () {

        float bearing=0;
        //Calculate the distance [meters]:
        float x = 0, y = 0;
        float deltaDistance = Haversine(ref latitude, ref longitude, ref bearing, ref x, ref y) * 1000f;
        if ( (deltaDistance > 1) && (deltaDistance<1000)  )
        {
            distance += deltaDistance;
            curr_deltaDistance = deltaDistance;
        }

        //Estimate speed from the delta distance between two location updates:
        float speed_est = CalcVelocity(deltaDistance);

        //Find closest attractions:
        if(LocationChanged() && (deltaDistance > LOC_DELTA_DISTANCE) && (deltaDistance<1000))
        {
            //Get closest attraction:
            //GetClosestAttraction(latitude, longitude);
            //StartCoroutine(GetClosestAttraction(latitude, longitude));
            CurrentLoc.lat = latitude; CurrentLoc.lng = longitude;

            if (Closest_attraction.valid)
            {
                //Get direction to target:
                GetDirection(ref CurrentLoc, ref Closest_attraction.location, ref DirectionToTarget);
                //TO DO: calculate the x, y of the camera and create a function that
                // places the VR object relative to the camera position in x, y
                //StartCoroutine(CreateAttractionInVR(DirectionToTarget.x, DirectionToTarget.y, "Billboard110"));
            }
        }


        //Move player if..:
        //if ( (deltaDistance < 1000) && (deltaDistance>0))
        if ( deltaDistance < 1000)
        {

            if (IsSimMode)
            {
                GenSmoothedMovement(deltaDistance, bearing, speed_est,x,y);
                //transform.Translate(Vector3.forward * ((float)deltaDistance * Mathf.Cos(bearing * Mathf.Deg2Rad)) / 100);//Move left 1/50 meters
                //transform.Translate(Vector3.right * ((float)(deltaDistance) * Mathf.Sin(bearing * Mathf.Deg2Rad)) / 100);//Move right 1/50 meters
            }
            else
            {

                GenSmoothedMovement(deltaDistance, bearing, speed_est,x,y);
                //float true_heading =
                //transform.rotation  = Quaternion.Euler(0, -Input.compass.trueHeading, 0);

                //transform.Translate(Vector3.forward * ((float)deltaDistance * Mathf.Cos(bearing * Mathf.Deg2Rad)) / 5);//Move left 1/50 meters
                //transform.Translate(Vector3.right * ((float)(deltaDistance) * Mathf.Sin(bearing * Mathf.Deg2Rad)) / 5);//Move right 1/50 meters
            }

        }

        float speed_kmph = speed_est * 3.6f;
        Debug.Log(
            "B=" + bearing.ToString("F2") + 
            " DeltDist=" + deltaDistance.ToString("F2")+
            " LAT="+route_lat[route_index].ToString("F2")+ 
            " LON"+ route_lon[route_index].ToString("F2")+
            " SPEED="+ speed_kmph.ToString("F2")+
            " DTIME="+ delta_loc_time_changed.ToString("F2")
            );
    
        //Synthetic movement:
        //transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);


    }
}


//-------------------------------------------------------------------------------------------------
/*
 /*
        float deltaLatitude = (newLatitude - lastLatitude) * Mathf.Deg2Rad;
        float deltaLontitude = (newLontitude - lastLontitude) * Mathf.Deg2Rad;
        float a = Mathf.Pow(Mathf.Sin(deltaLatitude / 2), 2) +
            Mathf.Cos(lastLatitude * Mathf.Deg2Rad) * Mathf.Cos(newLatitude * Mathf.Deg2Rad) *
            Mathf.Pow(Mathf.Sin(deltaLontitude / 2), 2);

        
        y = Mathf.Sin(deltaLontitude) * Mathf.Cos(newLatitude * Mathf.Deg2Rad);
        x = Mathf.Cos(lastLatitude * Mathf.Deg2Rad) * Mathf.Sin(newLatitude * Mathf.Deg2Rad) -
                Mathf.Sin(lastLatitude * Mathf.Deg2Rad) * Mathf.Cos(newLatitude * Mathf.Deg2Rad) * Mathf.Cos(deltaLontitude);
        
        //Calculate the bearing:
        if (LocationChanged())
        {
            //If location changed, updated the bearing:
            float bearing_ = Mathf.Atan2(y, x);
            //Bearing is in degrees:
            bearing = bearing_ * Mathf.Rad2Deg;
            approx_bearing = bearing;
        }
        else //Assume the same bearing as before:
        {
            bearing = approx_bearing;
        }

        lastLatitude = newLatitude;
        lastLontitude = newLontitude;
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return EARTH_RADIUS * c;
        
 *
 * 
 * //Calculate the bearing:
        /*var y = Math.sin(λ2 - λ1) * Math.cos(φ2);
        var x = Math.cos(φ1) * Math.sin(φ2) -
                Math.sin(φ1) * Math.cos(φ2) * Math.cos(λ2 - λ1);
        var brng = Math.atan2(y, x).toDegrees();
        
 /Class for handling arrays:
    [System.Serializable]
    public class JsonHelper
    {
        public static T[] getJsonArray<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            JSONArrayWrapper<T> wrapper = JsonUtility.FromJson<JSONArrayWrapper<T>>(newJson);
            return wrapper.array;
        }

        [System.Serializable]
        private class JSONArrayWrapper<T>
        {
            public T[] array;
        }
    }
 * 
        //UnityWebRequest json = UnityWebRequest.Get(url);

        //yield return json;
        //string jsonString = json.ToString();
Debug.Log(Application.dataPath.ToString());
       //jsonString = System.Text.Encoding.UTF8.GetString(json.bytes, 3, json.bytes.Length - 3);
        //jsonString = System.Text.Encoding.UTF8.GetString(json.bytes);
 
 * string json_string = File.ReadAllText("C:\\Users\\kosta\\Documents\\JSON_TXT.txt");
 * System.IO.File.WriteAllText("C:\\Users\\kosta\\Documents\\JSON_TXT.txt", json.text);
        //TODO:
        //PlacesApiQueryResponse ClosestAttraction = JsonHelper.getJsonArray<PlacesApiQueryResponse>(jsonString);
        //var jsonString = File.ReadAllText(Application.dataPath + "/Resources/data/terrainial_content.json");
        //PlacesApiQueryResponse ClosestAttraction = JsonUtility.FromJson<PlacesApiQueryResponse>(jsonString);

 * 
 * //jsonString = System.Text.Encoding.UTF8.GetString(json.bytes, 3, json.bytes.Length - 3);
        //jsonString = System.Text.Encoding.UTF8.GetString(json.bytes);
        Debug.Log("Got Json: " + json.text);
        System.IO.File.WriteAllText("C:\\Users\\kosta\\Documents\\JSON_TXT.txt", json.text);
        //TODO:
        //PlacesApiQueryResponse ClosestAttraction = JsonHelper.getJsonArray<PlacesApiQueryResponse>(jsonString);
        //var jsonString = File.ReadAllText(Application.dataPath + "/Resources/data/terrainial_content.json");
        //PlacesApiQueryResponse ClosestAttraction = JsonUtility.FromJson<PlacesApiQueryResponse>(jsonString);
        Debug.Log(Application.dataPath.ToString());
        string json_string = File.ReadAllText("C:\\Users\\kosta\\Documents\\JSON_TXT.txt");
        // PlacesApiQueryResponse ClosestAttraction = JsonUtility.FromJson<PlacesApiQueryResponse>(json.text);
        PlacesApiQueryResponse ClosestAttraction = JsonUtility.FromJson<PlacesApiQueryResponse>(json.text);
 * 
 * 
 * 
using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync(string.Format("https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={0},{1}&radius=500&type=bar&key=YourAPIKey", latitude, longitude));
var result = JsonConvert.DeserializeObject<PlacesApiQueryResponse>(response);
        }    
*/
//Wait for 100 seconds:
//System.Threading.Thread.Sleep(500);

//float true_heading = 180; //deg
//transform.rotation = Quaternion.Euler(0, true_heading, 0);
//Set new position:
//transform.Translate(Vector3.forward * ((float)deltaDistance * Mathf.Cos(true_heading * Mathf.Deg2Rad)) / 100);//Move left 1/50 meters
//transform.Translate(Vector3.right * ((float)(deltaDistance) * Mathf.Sin(true_heading* Mathf.Deg2Rad)) / 100);//Move right 1/50 meters

//OBJECT PARSER FROM GOOGLE LOCATION:
/*
    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Northeast
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Southwest
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Viewport
    {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Geometry
    {
        public Location location { get; set; }
        public Viewport viewport { get; set; }
    }

    public class OpeningHours
    {
        public bool open_now { get; set; }
        public List<object> weekday_text { get; set; }
    }

    public class Photo
    {
        public int height { get; set; }
        public List<string> html_attributions { get; set; }
        public string photo_reference { get; set; }
        public int width { get; set; }
    }

    public class Result
    {
        public Geometry geometry { get; set; }
        public string icon { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public OpeningHours opening_hours { get; set; }
        public List<Photo> photos { get; set; }
        public string place_id { get; set; }
        public double rating { get; set; }
        public string reference { get; set; }
        public string scope { get; set; }
        public List<string> types { get; set; }
        public string vicinity { get; set; }
    }

    public class RootObject1
    {
        public List<object> html_attributions { get; set; }
        public string next_page_token { get; set; }
        public List<Result> results { get; set; }
        public string status { get; set; }
    }

    */




/*
    public double[] route_lat = {
                                    32.11593,
                                    32.11622,
                                    32.11651,
                                    32.1168,
                                    32.11738,
                                    32.11593,
                                    32.11462,
                                    32.11451,
                                    32.11462,
                                    32.11458,
                                    32.11473,
                                    32.11466,
                                    32.1148,
                                    32.11466,
                                    32.11458
    };

    public double[] route_lon =
    {                       34.93693,
                            34.93384,
                            34.93041,
                            34.92714,
                            34.92354,
                            34.92337,
                            34.92285,
                            34.9244,
                            34.92586,
                            34.92732,
                            34.92912,
                            34.93088,
                            34.93247,
                            34.93427,
                            34.93594
     };




*/
//latitude = Input.location.lastData.latitude;
//longitude = Input.location.lastData.longitude;

//For Testing:
//latitude = (float)route_lat[route_index];
//longitude = (float)route_lon[route_index];

//transform.Translate(Vector3.right * ((float)(deltaDistance) * Mathf.Sin(((float)(90*3.14/180)))) / 50);//Move right 1/50 meters

//transform.Translate(Vector3.forward * (float)(deltaDistance));
// transform.Translate(Vector3.forward * ((float)deltaDistance * Mathf.Cos(bearing)) / 50);//Move left 1/50 meters
// transform.Translate(Vector3.right * ((float)(deltaDistance) * Mathf.Sin(bearing)) / 50);//Move right 1/50 meters



//bearing = Mathf.Atan(y /x);//In radians
/*TODO:
  Since atan2 returns values in the range -π... + π(that is, -180° ... +180°), 
   to normalise the result to a compass bearing(in the range 0° ... 360°, with −ve values transformed into the range 180° ... 360°), 
     convert to degrees and then use(θ+360) % 360, where % is (floating point) modulo.
  For final bearing, simply take the initial bearing from the end point to the start point and reverse it(using θ = (θ + 180) % 360).

  //bearing_ = (bearing_*Mathf.Rad2Deg + 360)%360;
  //bearing = (bearing_ + 180) % 360;
 //--TODO*/

//float newLatitude = Input.location.lastData.latitude;
//float newLontitude = Input.location.lastData.longitude;

/*definition of the variables:
    var φ1 = lat1.toRadians();
    var φ2 = lat2.toRadians();
    var Δφ = (lat2-lat1).toRadians();
    var Δλ = (lon2-lon1).toRadians();
 */



/*
    //Get XY point from Lat Lon [meters] 
    void getPoint(lat, lon, mapwidth, mapheight)
    {
        x = (180 + lon) * (mapwidth / 360);
        y = (90 - lat) * (mapheight / 180);
    }
    */




//Clone the Roadblock:
//GameObject Grass_ = GameObject.Find("Grass");
/*if (false)
{
    //Duplicate the road blocks:
    //RectTransform Grass_ = GetComponent<RectTransform>();
    GameObject clone;
    clone = Instantiate(Grass_, transform.position, transform.rotation) as GameObject;
    clone.transform.Translate(Vector3.forward * 12);
    fToDuplicate = false;
}
*/
//Move forward relatively to gps location distance:
//transform.Translate(Vector3.forward * (float)(deltaDistance/10));




/*public double [] route_ = { 32.09657833, 34.97455667, 
                            32.09654833, 34.974505,
                            32.09658167, 34.97446167,
                            32.09658167, 34.97446167,
                            32.09659,    34.974385,
                            32.096605,   34.97435167,
                            32.096615,  34.97431833,
                            32.09663167, 34.974285,
                            32.09663167, 34.974285,
                            32.09665833, 34.97423167,
                            32.09667667, 34.97421,
                            32.09669167, 34.97417833,
                            32.09672 ,   34.97415,
                            32.09672 ,   34.97415,
                            32.09673667, 34.97412833,
                            32.09676167, 34.97411167,
                            32.096785,   34.97410667,
                            32.09680833, 34.97411,
                            32.09683333, 34.97412
                            };

    //Simulated waypoints:
    public double[] route_lat = {
                                     32.10606,
                                32.10609,
                                32.10611,
                                32.10612,
                                32.10613,
                                32.10611,
                                32.10609,
                                32.10606,
                                32.10602,
                                32.10599,
                                32.10598,
                                32.10597,
                                32.10597,
                                32.10597,
                                32.10599,
                                32.10604
    };

    public double[] route_lon =
    {
                        34.95228,
                        34.95234,
                        34.95241,
                        34.95249,
                        34.95257,
                        34.95263,
                        34.95266,
                        34.95271,
                        34.95273,
                        34.95269,
                        34.95261,
                        34.95253,
                        34.95248,
                        34.9524,
                        34.95234,
                        34.95227

    };

    
    */
