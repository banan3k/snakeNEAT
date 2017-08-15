using UnityEngine;
using System.Collections;
using SharpNeat.Phenomes;
using System.Collections.Generic;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using System;
using System.Xml;
using System.IO;

public class Optimizer : MonoBehaviour {

    const int NUM_INPUTS = 6;
    const int NUM_OUTPUTS = 3;

    public int Trials;
    public float TrialDuration;
    public float StoppingFitness;
    bool EARunning;
	string popFileSavePath, champFileSavePath, champFileSavePath2;

    SimpleExperiment experiment;
    static NeatEvolutionAlgorithm<NeatGenome> _ea;

    public GameObject Unit;

    Dictionary<IBlackBox, UnitController> ControllerMap = new Dictionary<IBlackBox, UnitController>();
    private DateTime startTime;
    private float timeLeft;
    private float accum;
    private int frames;
    private float updateInterval = 12;

    private uint Generation;
    private double Fitness;

	// Use this for initialization
	void Start () {
        Utility.DebugLog = true;
        experiment = new SimpleExperiment();
        XmlDocument xmlConfig = new XmlDocument();
        TextAsset textAsset = (TextAsset)Resources.Load("experiment.config");
        xmlConfig.LoadXml(textAsset.text);
        experiment.SetOptimizer(this);

        experiment.Initialize("Car Experiment", xmlConfig.DocumentElement, NUM_INPUTS, NUM_OUTPUTS);

        champFileSavePath = Application.persistentDataPath + string.Format("/{0}.champ.xml", "car");
        popFileSavePath = Application.persistentDataPath + string.Format("/{0}.pop.xml", "car");     

		champFileSavePath2 = Application.persistentDataPath + string.Format("/{0}.champ.xml", "snake");

        print(champFileSavePath);
	}

    // Update is called once per frame
    void Update()
    {
      //  evaluationStartTime += Time.deltaTime;
        if (Input.GetKey("escape"))
            Application.Quit();
        if (Input.GetKey("up"))
        {
			GameObject.Find("Main Camera").transform.GetComponent<Camera>().orthographicSize ++;
        }
		if (Input.GetKey("down"))
		{
			GameObject.Find("Main Camera").transform.GetComponent<Camera>().orthographicSize --;
		}

        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        if (timeLeft <= 0.0)
        {
            var fps = accum / frames;
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
            //   print("FPS: " + fps);
            if (fps < 10)
            {
                Time.timeScale = Time.timeScale - 1;
                print("Lowering time scale to " + Time.timeScale);
            }
        }
    }

    public void StartEA()
    {        
        Utility.DebugLog = true;
        Utility.Log("Starting PhotoTaxis experiment");
        // print("Loading: " + popFileLoadPath);
        _ea = experiment.CreateEvolutionAlgorithm(popFileSavePath);
        startTime = DateTime.Now;

        _ea.UpdateEvent += new EventHandler(ea_UpdateEvent);
        _ea.PausedEvent += new EventHandler(ea_PauseEvent);

        var evoSpeed = 20;


        Time.timeScale = evoSpeed;       
        _ea.StartContinue();
        EARunning = true;
    }

    void ea_UpdateEvent(object sender, EventArgs e)
    {

		Utility.Log(string.Format("gen={0:N0} bestFitness={1:N6}",
			_ea.CurrentGeneration, _ea.Statistics._maxFitness));



        Fitness = _ea.Statistics._maxFitness;

        Generation = _ea.CurrentGeneration;

		if (MaxAllFitness < Fitness) 
		{
			MaxAllFitness = (float)Fitness;

			float timeTemp = Time.timeScale;
			Time.timeScale = 0;
			AllGenerationBest = _ea.CurrentChampGenome;

			XmlWriterSettings _xwSettings = new XmlWriterSettings();
			_xwSettings.Indent = true;

			NeatGenome genome = null;

			bool fileAlreadyExist = File.Exists(champFileSavePath2);

			try
			{
			using (XmlReader xr = XmlReader.Create (champFileSavePath2)) {
				genome = NeatGenomeXmlIO.ReadCompleteGenomeList (xr, false, (NeatGenomeFactory)experiment.CreateGenomeFactory ()) [0];

			}
			}catch(Exception ex) {

			}

			using (XmlWriter xw = XmlWriter.Create(champFileSavePath2, _xwSettings))
			{

				if((fileAlreadyExist && genome.EvaluationInfo.Fitness<MaxAllFitness) || !fileAlreadyExist)
				{
					experiment.SavePopulation(xw, new NeatGenome[] { _ea.CurrentChampGenome });
					Debug.Log ("saved new best: "+AllGenerationBest.EvaluationInfo.Fitness);
				}




			}
			Time.timeScale = timeTemp;


//			Debug.Log(_ea.CurrentChampGenome.EvaluationInfo.Fitness+" vs "+AllGenerationBest.EvaluationInfo.Fitness);

		}

    }

	public NeatEvolutionAlgorithm<NeatGenome> getEA()
	{
		return _ea;
	}

	static NeatGenome[] temp = new NeatGenome[50];

	static float MaxAllFitness=0;
	static NeatGenome AllGenerationBest;

	private int GenerationBest;

    void ea_PauseEvent(object sender, EventArgs e)
    {
        Time.timeScale = 1;
        Utility.Log("Done ea'ing (and neat'ing)");

        XmlWriterSettings _xwSettings = new XmlWriterSettings();
        _xwSettings.Indent = true;
        // Save genomes to xml file.        
        DirectoryInfo dirInf = new DirectoryInfo(Application.persistentDataPath);
        if (!dirInf.Exists)
        {
            Debug.Log("Creating subdirectory");
            dirInf.Create();
        }
        using (XmlWriter xw = XmlWriter.Create(popFileSavePath, _xwSettings))
        {
            experiment.SavePopulation(xw, _ea.GenomeList);
        }
        // Also save the best genome

        using (XmlWriter xw = XmlWriter.Create(champFileSavePath, _xwSettings))
        {
            experiment.SavePopulation(xw, new NeatGenome[] { _ea.CurrentChampGenome });
        }


        DateTime endTime = DateTime.Now;
        Utility.Log("Total time elapsed: " + (endTime - startTime));

        System.IO.StreamReader stream = new System.IO.StreamReader(popFileSavePath);
       

      
        EARunning = false;        
        
    }

    public void StopEA()
    {

        if (_ea != null && _ea.RunState == SharpNeat.Core.RunState.Running)
        {
            _ea.Stop();

        }
    }



    public void Evaluate(IBlackBox box)
    {
        GameObject obj = Instantiate(Unit, Unit.transform.position, Unit.transform.rotation) as GameObject;
        UnitController controller = obj.GetComponent<UnitController>();

        ControllerMap.Add(box, controller);

        controller.Activate(box);
    }

    public void StopEvaluation(IBlackBox box)
    {
        UnitController ct = ControllerMap[box];

        Destroy(ct.gameObject);
		if(GameObject.Find ("AllPoints"))
		GameObject.Find ("AllPoints").SetActive(false);
    }

	public void RunBest(int mode) //0 for normal best, 1 for experimental best
    {
        Time.timeScale = 1;

        NeatGenome genome = null;

		Debug.Log (champFileSavePath);
		string FileString;

		Color visualEffect = Color.blue;

		if (mode == 0)
			FileString = champFileSavePath;
		else {
			FileString = champFileSavePath2;
			visualEffect = Color.red;
		}
        // Try to load the genome from the XML document.
        try
        {
			using (XmlReader xr = XmlReader.Create(FileString))
                genome = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, (NeatGenomeFactory)experiment.CreateGenomeFactory())[0];


        }
        catch (Exception e1)
        {
            // print(champFileLoadPath + " Error loading genome from file!\nLoading aborted.\n"
            //						  + e1.Message + "\nJoe: " + champFileLoadPath);
			Debug.Log("get error in best");
            return;
        }

        // Get a genome decoder that can convert genomes to phenomes.
        var genomeDecoder = experiment.CreateGenomeDecoder();

        // Decode the genome into a phenome (neural network).
        var phenome = genomeDecoder.Decode(genome);

        GameObject obj = Instantiate(Unit, Unit.transform.position, Unit.transform.rotation) as GameObject;
        UnitController controller = obj.GetComponent<UnitController>();

		obj.GetComponent<SpriteRenderer> ().color = visualEffect;

        ControllerMap.Add(phenome, controller);

        controller.Activate(phenome);
    }

    public float GetFitness(IBlackBox box)
    {
        if (ControllerMap.ContainsKey(box))
        {
            return ControllerMap[box].GetFitness();
        }
        return 0;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 40), "Start EA"))
        {
            StartEA();
        }
        if (GUI.Button(new Rect(10, 60, 100, 40), "Stop EA"))
        {
            StopEA();
        }
        if (GUI.Button(new Rect(10, 110, 100, 40), "Run best"))
        {
			
			RunBest(0);
			RunBest(1);

        }

        GUI.Button(new Rect(10, Screen.height - 70, 100, 60), string.Format("Generation: {0}\nFitness: {1:0.00}", Generation, Fitness));
    }
}
