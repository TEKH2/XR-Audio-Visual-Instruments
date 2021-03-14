using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class ActorSpawner : MonoBehaviour {

	public ObiActor template;

	public int basePhase = 2;
	public int maxInstances = 15;
	public float spawnDelay = 0.3f;

	private int phase = 0;
	private int instances = 0;
	private float timeFromLastSpawn = 0;
	
	// Update is called once per frame
	void Update () {

		timeFromLastSpawn += Time.deltaTime;

		if (Input.GetMouseButtonDown(0) && instances < maxInstances && timeFromLastSpawn > spawnDelay)
		{
			GameObject go = GameObject.Instantiate(template.gameObject,transform.position,Quaternion.identity);
			go.GetComponent<ObiActor>().SetPhase(Oni.MakePhase(basePhase + phase,0));

			phase++;
			instances++;
			timeFromLastSpawn = 0;
		}
	}
}
