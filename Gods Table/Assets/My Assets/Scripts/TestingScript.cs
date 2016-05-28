using UnityEngine;
using System.Collections;
using PoissonDiscGeneration;
using System.Collections.Generic;

public class TestingScript : MonoBehaviour
{
    [SerializeField]
    private float width = 10;
    [SerializeField]
    private float height = 10;
    [SerializeField]
    private float minimumPointDistance = 0.25f;
    [SerializeField]
    private int maximumAttempts = 30;

    private PoissonDiscGenerator gen;
    private List<Vector2> points;

	// Use this for initialization
	void Start ()
	{
	    gen = new PoissonDiscGenerator(width, height, minimumPointDistance, maximumAttempts);
	    points = gen.BlockedGeneratePoints2D();
	}

    void OnDrawGizmos()
    {
        if (points != null)
        {
            foreach (var point in points)
            {
                Gizmos.DrawSphere(point, 0.05f);
            }
        }
    }
}
