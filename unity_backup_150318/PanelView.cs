using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelView : MonoBehaviour
{

    
    public TextMesh CoordinateText;
    //Earth radius in KM:
    const float EARTH_RADIUS = 6371;
    public float distance = 0;
    public bool IsNorthIsrael = true;
    public float latitude = 0, longitude = 0;

    public GameObject MainCamera;

    public Vector3 previousPos;
    public float velocityVR;
    public float velocityReal;
    public float previousDistance;


    // Use this for initialization
    void Start()
    {

        
        //Get the GUI object of the panel text from canvas:
        CoordinateText = GetComponent<TextMesh>();

        //Initialize the GPS location service:
        Input.location.Start();
        //Set initial location:
        latitude = Input.location.lastData.latitude;
        longitude = Input.location.lastData.longitude;


        //latitude = (float)32.288076;
        //longitude = (float)34.869232;

        //(32.288076, 34.869232)


        //clone = Instantiate(projectile, transform.position, transform.rotation) as Rigidbody;
        //clone.velocity = transform.TransformDirection(Vector3.forward * 1);

    }


    /* Params: lat1, long1 => Latitude and Longitude of current point
    *         lat2, long2 => Latitude and Longitude of target  point
    *         headX       => x-Value of built-in phone-compass
    * Returns the degree of a direction from current point to target point
    */
    float getDegrees(ref float lat1, ref float lon1, ref float lat2, ref float lon2)
    {

        float dLat = Mathf.Deg2Rad * (lat2 - lat1);
        float dLon = Mathf.Deg2Rad * (lon2 - lon1);

        lat1 = Mathf.Deg2Rad * (lat1);
        lat2 = Mathf.Deg2Rad * (lat2);

        float y = Mathf.Sin(dLon) * Mathf.Cos(lat2);
        float x = Mathf.Cos(lat1) * Mathf.Sin(lat2) -
                Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon);
        float brng = Mathf.Rad2Deg * (Mathf.Atan2(y, x));

        // fix negative degrees
        if (brng < 0)
        {
            brng = 360 - Mathf.Abs(brng);
        }

        //return (brng - headX);
        return (brng);
    }



    //Function calculates the distance in meters from lat lon
    float Haversine(ref float lastLatitude, ref float lastLontitude)
    {
        float newLatitude = Input.location.lastData.latitude;
        float newLontitude = Input.location.lastData.longitude;
        float deltaLatitude = (newLatitude - lastLatitude) * Mathf.Deg2Rad;
        float deltaLontitude = (newLontitude - lastLontitude) * Mathf.Deg2Rad;
        float a = Mathf.Pow(Mathf.Sin(deltaLatitude / 2), 2) +
            Mathf.Cos(lastLatitude * Mathf.Deg2Rad) * Mathf.Cos(newLatitude * Mathf.Deg2Rad) *
            Mathf.Pow(Mathf.Sin(deltaLontitude / 2), 2);
        lastLatitude = newLatitude;
        lastLontitude = newLontitude;
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return EARTH_RADIUS * c;

    }

    // Update is called once per frame
    void Update()
    {

        //Location status parameter
        string LocStatus = "Empty";

        //Move the "Father" of the script:
        //transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);





        CoordinateText = GetComponent<TextMesh>();
        if (CoordinateText != null)
        {
            if (Input.location.status == LocationServiceStatus.Failed)
                LocStatus = "LocationServiceStatus.Failed";
            if (Input.location.status == LocationServiceStatus.Stopped)
                LocStatus = "LocationServiceStatus.Stopped";
            if (Input.location.status == LocationServiceStatus.Running)
                LocStatus = "LocationServiceStatus.Running";
            if (Input.location.status == LocationServiceStatus.Initializing)
                LocStatus = "LocationServiceStatus.Initializing";

            if (!Input.location.isEnabledByUser)
                LocStatus = LocStatus.ToString() + " UserRefused ";
            else LocStatus = LocStatus.ToString() + " UserEnabled ";

            //Calculate the distance:
            float deltaDistance = Haversine(ref latitude, ref longitude) * 1000f;
            if (deltaDistance > 0 && (latitude != 0) && (deltaDistance<1000))
            {
                distance += deltaDistance;
            }
            //int coins = (int)(distance / 100f);//Coins tic is 1 to 100 meters

            //Calculate the real velocity:[m/sec]
            velocityReal = (distance - previousDistance) / Time.deltaTime;
            previousDistance = distance;

            //transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);

            //Calculate the VR velocity:[tic/sec]
            velocityVR = ((transform.position - previousPos).magnitude) / Time.deltaTime;
            previousPos = transform.position;

            //+ " Time =  " + Time.deltaTime.ToString();
            Debug.Log(CoordinateText.text.ToString());

            //Get the position of the camera:

            //Camera camera_ = GetComponent<Camera> ();
            MainCamera = GameObject.Find("MainCamera");
            Vector3 p = MainCamera.transform.position;
            float x = transform.position.x;
            float y = transform.position.y;
            float z = transform.position.z;

            //CoordinateText.text = "Distance = " + distance.ToString() + ", DELTA= " + deltaDistance.ToString() +
            //    " LAT=" + Input.location.lastData.latitude.ToString() +
            //    " LON=" + Input.location.lastData.longitude.ToString();

            /*CoordinateText.text = "Distance = " + distance.ToString() + 
                ", X= " + x.ToString() + " Y=" + y.ToString() + " Z=" + z.ToString()+
                "CarVel="+ velocityReal.ToString()+" VRVel="+ velocityVR.ToString();
            */
            //CoordinateText.text = 
                //"Distance = " + distance.ToString() +
                //", X= " + x.ToString() + " Y=" + y.ToString() + " Z=" + z.ToString() +
             //   "CarVel=" + velocityReal.ToString() + " VRVel=" + velocityVR.ToString() +
             //   " Dist=" + distance.ToString() ;

            GameObject thePlayer = GameObject.Find("Player1");
            MovePlayer playerScript = thePlayer.GetComponent<MovePlayer>();

            string attraction = "";
            attraction = attraction + "ATTSTAT=" + playerScript.Closest_attraction.status + "“\n”";
            if (playerScript.Closest_attraction.name != null)
            {
                attraction = playerScript.Closest_attraction.name.ToString() + "“\n”";
                if (playerScript.DirectionToTarget != null)
                    attraction = attraction + " ATTD="+
                        playerScript.DirectionToTarget.distance.ToString("F2") + "“\n”"+
                        " ATTBr=" + playerScript.DirectionToTarget.bearing.ToString("F2") + "“\n”"+
                        " ATTBRank="+ playerScript.Closest_attraction.rating.ToString("F2");


            }
                    //" ATT_BER="+ playerScript.DirectionToTarget.bearing.ToString("F2");

                    CoordinateText.text = "VEL=" + playerScript.approx_velocity.ToString("F2") +
                              " BEAR=" + playerScript.approx_bearing.ToString("F2") + "“\n”" +
                              " DTIME=" + playerScript.delta_loc_time_changed.ToString("F2") +
                              "DDist=" + playerScript.curr_deltaDistance.ToString("F2") + "“\n”" +
                              " DIST=" + playerScript.distance.ToString("F2")+ "“\n”"+
                              " ATTNAME="+ attraction;
                                  
                                  /*"ATNAME=" + playerScript.Closest_attraction.name.ToString() + "“\n”" +
                                  "ATDIST=" + playerScript.DirectionToTarget.distance.ToString("F2") + "“\n”" +
                                  "ATBEAR=" + playerScript.DirectionToTarget.bearing.ToString("F2");
                                  */
            

            //Debug.Log("Delta distance= " + deltaDistance.ToString());




        }

    }



}

