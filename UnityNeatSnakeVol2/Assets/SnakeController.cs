using UnityEngine;
using System.Collections;
using SharpNeat.Phenomes;

public class SnakeController : UnitController
{

    public float Speed = 5f;
    public float TurnSpeed = 180f;
    public int Lap = 1;
    public int CurrentPiece, LastPiece;
    bool MovingForward = true;
    bool IsRunning = true;
    public float SensorRange = 10;
    float SensorRangeSnake = 10;//.2f;
    int WallHits;
    IBlackBox box;

    LineRenderer snakeTail;

    int whatSideToMove = 0;
    float gasMove = 0.2f;

    void Start()
    {


        gameObject.AddComponent<BoxCollider>().isTrigger = true;
        snakeTail = GetComponent<LineRenderer>();
        snakeTail.SetVertexCount(tailLength);
        oldTail = new Vector3[1000];

        addAnotherPointObject();
    }

    public Transform PointToCreate;
    int Score = 0;

    void OnTriggerEnter(Collider coll)
    {


        if (coll.gameObject.transform == seekingThisPoint)
        {
            coll.gameObject.SetActive(false);
            Score++;
            tailLength = Score + 1;

            addAnotherPointObject();
        }
    }

    void OnCollisionEnter(Collision coll)
    {

        string whatIDofPoint = "Point";

        if (coll.gameObject.tag == "Wall")
        {
            IsRunning = false;
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionX;
            timeUp = 0;
            if (seekingThisPoint)
                seekingThisPoint.gameObject.SetActive(false);
        }
    }

    void addAnotherPointObject()
    {
        int x = Random.Range(-8, 8);
        int y = Random.Range(-4, 4);
        Transform createdPoint = Instantiate(PointToCreate, new Vector3(x, y, 0), transform.rotation) as Transform;
        createdPoint.name = "Point" + Score;
        createdPoint.tag = "Point";

        if (!GameObject.Find("AllPoints"))
        {
            GameObject allPoints = new GameObject();
            allPoints.name = "AllPoints";
        }
        createdPoint.parent = GameObject.Find("AllPoints").gameObject.transform;
        createdPoint.transform.localScale = new Vector3(1, 1, 1);

        createdPoint.GetComponent<SphereCollider>().isTrigger = true;

        seekingThisPoint = createdPoint;

		if (this.GetComponent<SpriteRenderer> ().color == Color.red)
			createdPoint.GetComponent<MeshRenderer> ().material.color = Color.red;
		else
			createdPoint.GetComponent<MeshRenderer> ().material.color = Color.blue;
    }

    Transform seekingThisPoint;
    // Update is called once per frame
    void FixedUpdate()
    {


        if (IsRunning)
        {
            float frontSensor = 0;
            float leftFrontSensor = 0;
            float leftSensor = 0;
            float rightFrontSensor = 0;
            float rightSensor = 0;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.TransformDirection(transform.right * -1), out hit, SensorRangeSnake))
            {
                if (hit.collider.tag.Equals("Wall"))
                {
                    frontSensor = 1 - hit.distance / SensorRangeSnake;

                }
            }

            Vector3 rightFrontSensorVector = new Vector3(((transform.right.x * -1) + transform.up.x), ((transform.right.y * -1) + transform.up.y), ((transform.right.z * -1) + transform.up.z));

            if (Physics.Raycast(transform.position, transform.TransformDirection(rightFrontSensorVector), out hit, SensorRangeSnake))
            {
                if (hit.collider.tag.Equals("Wall"))
                {
                    rightFrontSensor = 1 - hit.distance / SensorRangeSnake;

                }
            }

            if (Physics.Raycast(transform.position, transform.TransformDirection(transform.up), out hit, SensorRangeSnake))
            {
                if (hit.collider.tag.Equals("Wall"))
                {
                    rightSensor = 1 - hit.distance / SensorRangeSnake;

                }
            }
            Vector3 leftFrontSensorVector = new Vector3(((transform.right.x * -1) + (transform.up.x * -1)), ((transform.right.y * -1) + (transform.up.y * -1)), ((transform.right.z * -1) + (transform.up.z * -1)));

            if (Physics.Raycast(transform.position, transform.TransformDirection(leftFrontSensorVector), out hit, SensorRangeSnake))
            {
                if (hit.collider.tag.Equals("Wall"))
                {
                    leftFrontSensor = 1 - hit.distance / SensorRangeSnake;

                }
            }

            if (Physics.Raycast(transform.position, transform.TransformDirection(transform.up), out hit, SensorRangeSnake))
            {
                if (hit.collider.tag.Equals("Wall"))
                {
                    leftSensor = 1 - hit.distance / SensorRangeSnake;

                }
            }

            float xFar = 0, yFar = 0, zRot = 0;
            float xFarSensor = 0, yFarSensor = 0;


            xFarSensor = (transform.position.x - seekingThisPoint.position.x) / 19;
            yFarSensor = (transform.position.y - seekingThisPoint.position.y) / 9;

            xFar = Vector3.Distance(new Vector3(transform.position.x, 0, 0), new Vector3(seekingThisPoint.position.x, 0, 0));
            yFar = Vector3.Distance(new Vector3(0, transform.position.y, 0), new Vector3(0, seekingThisPoint.position.y, 0));





            ISignalArray inputArr = box.InputSignalArray;

            timeUp += (Time.deltaTime);



            inputArr[0] = xFarSensor;
            inputArr[1] = yFarSensor;
            inputArr[2] = this.transform.rotation.z;

            inputArr[3] = Score;
            inputArr[4] = this.transform.position.x / 10f;
            inputArr[5] = this.transform.position.y / 4.5f;



            box.Activate();

            ISignalArray outputArr = box.OutputSignalArray;

            var steerL = (float)outputArr[0] * 2 - 1;
            var steerR = (float)outputArr[1] * 2 - 1;
            var steerS = (float)outputArr[2] * 2 - 1;

            int whatTurn = 0;

            if (countAnotherTurn == 0)
            {
                if (steerS < steerL || steerS < steerR)
                {
                    if (steerL > steerR)
                    {
                        whatTurn = 90;


                    } else
                    {
                        whatTurn = -90;

                    }
                }
            }




            if (countAnotherTurn == 0)
            {

                transform.Rotate(new Vector3(0, 0, whatTurn));

            }

            if (whatTurn != 0 || countAnotherTurn != 0)
            {
                countAnotherTurn++;
                whatTurn = 0;
            }
            if (countAnotherTurn == 4)
                countAnotherTurn = 0;

            this.transform.Translate(new Vector3(gasMove, 0, 0));


            Vector3[] tailFollow = new Vector3[tailLength];
            snakeTail.SetVertexCount(tailLength);
            if (tailLength != 0)
                tailFollow[0] = transform.position - (transform.right / 2);
            for (int i = 1; i < tailLength; i++)
            {
                if (i - 1 < oldTail.Length)
                    tailFollow[i] = oldTail[i - 1];


                if (Vector3.Distance(this.transform.position, tailFollow[i]) < 0.5f)
                {
                    IsRunning = false;
                    if (seekingThisPoint)
                        seekingThisPoint.gameObject.SetActive(false);
                }


            }

            if (tailFollow.Length - 2 >= 0 && tailFollow[tailFollow.Length - 1].x == 0)
                tailFollow[tailFollow.Length - 1] = tailFollow[tailFollow.Length - 2];
            snakeTail.SetPositions(tailFollow);

            oldTail = tailFollow;

        }

    }

    int zmianaZdania = 0;

    float oldTurn = 0;
    int countTurnOld = 0;

    int countAnotherTurn = 0;
    float oldOut;
    int oldNumbers = 0;

    int tailLength = 0, maxTailLength = 3;
    Vector3[] oldTail;

    public override void Stop()
    {
        this.IsRunning = false;
    }

    public override void Activate(IBlackBox box)
    {
        this.box = box;
        this.IsRunning = true;
    }

    public void NewLap()
    {
        if (LastPiece > 2 && MovingForward)
        {
            Lap++;
        }
    }

    float timeUp = 0;

    public override float GetFitness()
    {

        float distanceFromPoint = Vector3.Distance(this.transform.position, seekingThisPoint.transform.position) / 22;

        return Score;
    }


    void OnGUI()
    {
        if(IsRunning)
        {
			if (this.GetComponent<SpriteRenderer>().color == Color.red)
                GUI.Button(new Rect(10, 250, 95, 50), "Fitnes exp: " + Score);
            if (this.transform.GetComponent<SpriteRenderer>().color == Color.blue)
                GUI.Button(new Rect(10, 200, 95, 50), "Fitnes org: " + Score);
        }
        else
        {
            if (this.GetComponent<SpriteRenderer>().color == Color.red)
                GUI.Button(new Rect(10, 350, 115, 50), "Old Fitnes exp: " + Score);
            if (this.transform.GetComponent<SpriteRenderer>().color == Color.blue)
                GUI.Button(new Rect(10, 300, 115, 50), "Old Fitnes org: " + Score);
        }
    }

}
