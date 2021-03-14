using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Obi{

[ExecuteInEditMode]
public class ObiCurve : MonoBehaviour {

	[Serializable]
	public struct ControlPoint{

		public enum BezierCPMode {
			Aligned,
			Mirrored,
			Free,
		}

		public Vector3 position;
		public Vector3 normal;
		public Vector3 inTangent;
		public Vector3 outTangent;

		public BezierCPMode tangentMode;
	
		public ControlPoint(Vector3 position, Vector3 normal){
			this.position = position;
			this.normal = normal;
			this.inTangent = -Vector3.right* 0.33f;
			this.outTangent = Vector3.right* 0.33f;
			this.tangentMode = BezierCPMode.Aligned;
		}

		public ControlPoint(Vector3 position, Vector3 normal, Vector3 inTangent, Vector3 outTangent, BezierCPMode tangentMode){
			this.position = position;
			this.normal = normal;
			this.inTangent = inTangent;
			this.outTangent = outTangent;
			this.tangentMode = tangentMode;
		}

		public ControlPoint Transform (Vector3 translation, Quaternion rotation){
			return new ControlPoint(position+translation,normal, rotation*inTangent, rotation*outTangent, tangentMode);
		}

		public ControlPoint Transform(Transform t){
				return new ControlPoint(t.TransformPoint(position),t.TransformVector(normal),t.TransformVector(inTangent),t.TransformVector(outTangent), tangentMode);
		}

		public ControlPoint InverseTransform(Transform t){
			return new ControlPoint(t.InverseTransformPoint(position),t.InverseTransformVector(normal),t.InverseTransformVector(inTangent),t.InverseTransformVector(outTangent), tangentMode);
		}
	
		// return tangent in world space:
		public Vector3 GetInTangent(){
			return position + inTangent;
		}

		// return tangent in local space:
		public Vector3 GetOutTangent(){
			return position + outTangent;
		}		

		public ControlPoint SetInTangent(Vector3 tangent){

			Vector3 newTangent = tangent - position;

			switch(tangentMode){
				case BezierCPMode.Mirrored: outTangent = -newTangent; break;
				case BezierCPMode.Aligned: outTangent = -newTangent.normalized * outTangent.magnitude; break;
			}

			return new ControlPoint(position,normal,newTangent,outTangent,tangentMode);
		}

		public ControlPoint SetOutTangent(Vector3 tangent){

			Vector3 newTangent = tangent - position;

			switch(tangentMode){
				case BezierCPMode.Mirrored: inTangent = -newTangent; break;
				case BezierCPMode.Aligned: inTangent = -newTangent.normalized * inTangent.magnitude; break;
			}

			return new ControlPoint(position,normal,inTangent,newTangent,tangentMode);
		}
	}

	protected const int arcLenghtSamples = 20;

	public bool closed = false;
	public List<ControlPoint> controlPoints = null;
	[HideInInspector][SerializeField] protected List<float> arcLengthTable = null;	
	[HideInInspector][SerializeField] protected float totalSplineLenght = 0.0f;


	/**
	* Returns world-space spline lenght.
	*/
	public float Length{
		get{return totalSplineLenght;}
	}

	public int MinPoints{
		get{return 2;}
	}

	public void Awake(){

		if (controlPoints == null){
			controlPoints = new List<ControlPoint>(){
								new ControlPoint(Vector3.left,Vector3.up),
								new ControlPoint(Vector3.zero,Vector3.up)}
								;
		}

		if (arcLengthTable == null){
			arcLengthTable = new List<float>();
			RecalculateSplineLenght(0.00001f,7);
		}

	}

	public int GetNumSpans(){
		
		if (controlPoints == null || controlPoints.Count < MinPoints) 
			return 0;

		return closed ? controlPoints.Count : controlPoints.Count-1;

	}

	public int AddPoint(float mu){

		if (controlPoints.Count >= MinPoints){

			if (!System.Single.IsNaN(mu)){

				float p;
				int i = GetSpanControlPointForMu(mu,out p);
				Vector3 normal = GetNormalAt(mu);
		
				int next = (i+1) % controlPoints.Count;

				Vector3 P0_1 = (1-p)*controlPoints[i].position + p*controlPoints[i].GetOutTangent();
				Vector3 P1_2 = (1-p)*controlPoints[i].GetOutTangent() + p*controlPoints[next].GetInTangent();
				Vector3 P2_3 = (1-p)*controlPoints[next].GetInTangent() + p*controlPoints[next].position;
				
				Vector3 P01_12 = (1-p)*P0_1 + p*P1_2;
				Vector3 P12_23 = (1-p)*P1_2 + p*P2_3;
				
				Vector3 P0112_1223 = (1-p)*P01_12 + p*P12_23;

				controlPoints[i] = controlPoints[i].SetOutTangent(P0_1);
				controlPoints[next] = controlPoints[next].SetInTangent(P2_3);

				controlPoints.Insert(i+1, new ControlPoint(P0112_1223,normal,P01_12 - P0112_1223,P12_23 - P0112_1223,ControlPoint.BezierCPMode.Aligned));

				return i+1;
			}
		}
		return -1;

	}

	public float GetClosestMuToPoint(Vector3 point,float samples){
		
		if (controlPoints.Count >= MinPoints){
	
			samples = Mathf.Max(1,samples);
			float step = 1/(float)samples;
			int numSpans = GetNumSpans();

			float closestMu = 0;
			float minDistance = float.MaxValue;

			Matrix4x4 l2w = transform.localToWorldMatrix;

			for(int k = 0; k < controlPoints.Count; ++k) {

				Vector3 _p = l2w.MultiplyPoint3x4(controlPoints[k].position);
				Vector3 p = l2w.MultiplyPoint3x4(controlPoints[k].GetOutTangent());
				Vector3 p_ = l2w.MultiplyPoint3x4(controlPoints[k+1].GetInTangent());
				Vector3 p__ = l2w.MultiplyPoint3x4(controlPoints[k+1].position);

				Vector3 lastPoint = Evaluate3D(_p,p,p_,p__,0);
				for(int i = 1; i <= samples; ++i){

					Vector2 currentPoint = Evaluate3D(_p,p,p_,p__,i*step);

					float mu;
					float distance = Vector2.SqrMagnitude(ObiUtils.ProjectPointLine(point,lastPoint,currentPoint,out mu) - point);

					if (distance < minDistance){
						minDistance = distance;
						closestMu = ((k-1) + (i-1)*step + mu/samples) / (float)numSpans;
					}
					lastPoint = currentPoint;
				}

			}

			return closestMu;

		}else{
			Debug.LogWarning("Catmull-Rom spline needs at least 4 control points to be defined.");
		}
		return 0;
	}


	/**
	 * Recalculates spline arc lenght in world space using Gauss-Lobatto adaptive integration. 
	 * @param acc minimum accuray desired (eg 0.00001f)
	 * @param maxevals maximum number of spline evaluations we want to allow per segment.
	 */
	public float RecalculateSplineLenght(float acc, int maxevals){
		
		totalSplineLenght = 0.0f;
		arcLengthTable.Clear();
		arcLengthTable.Add(0);

		float step = 1/(float)(arcLenghtSamples+1);

		if (controlPoints.Count >= MinPoints){

			Matrix4x4 l2w = transform.localToWorldMatrix;

			for(int k = 0; k < GetNumSpans(); ++k) {

				Vector3 _p = l2w.MultiplyPoint3x4(controlPoints[k].position);
				Vector3 p = l2w.MultiplyPoint3x4(controlPoints[k].GetOutTangent());
				Vector3 p_ = l2w.MultiplyPoint3x4(controlPoints[(k+1) % controlPoints.Count].GetInTangent());
				Vector3 p__ = l2w.MultiplyPoint3x4(controlPoints[(k+1) % controlPoints.Count].position);

				for(int i = 0; i <= Mathf.Max(1,arcLenghtSamples); ++i){

					float a = i*step;
					float b = (i+1)*step;

					float segmentLength = GaussLobattoIntegrationStep(_p,p,p_,p__,a,b,
				                                                 EvaluateFirstDerivative3D(_p,p,p_,p__,a).magnitude,
				                                                 EvaluateFirstDerivative3D(_p,p,p_,p__,b).magnitude,0,maxevals,acc);

					totalSplineLenght += segmentLength;

					arcLengthTable.Add(totalSplineLenght);

				}

			}
		}else{
			Debug.LogWarning("Catmull-Rom spline needs at least 4 control points to be defined.");
		}

		return totalSplineLenght;
	}


	/**
	 * One step of the adaptive integration method using Gauss-Lobatto quadrature.
	 * Takes advantage of the fact that the arc lenght of a vector function is equal to the
	 * integral of the magnitude of first derivative.
	 */
	private float GaussLobattoIntegrationStep(Vector3 p1,Vector3 p2,Vector3 p3,Vector3 p4, 
	                                          float a, float b,
	                                          float fa, float fb, int nevals, int maxevals, float acc){

		if (nevals >= maxevals) return 0;

		// Constants used in the algorithm
		float alpha = Mathf.Sqrt(2.0f/3.0f); 
		float beta  = 1.0f/Mathf.Sqrt(5.0f);
		
		// Here the abcissa points and function values for both the 4-point
		// and the 7-point rule are calculated (the points at the end of
		// interval come from the function call, i.e., fa and fb. Also note
		// the 7-point rule re-uses all the points of the 4-point rule.)
		float h=(b-a)/2; 
		float m=(a+b)/2;
		
		float mll=m-alpha*h; 
		float ml =m-beta*h; 
		float mr =m+beta*h; 
		float mrr=m+alpha*h;
		nevals += 5;
		
		float fmll= EvaluateFirstDerivative3D(p1,p2,p3,p4,mll).magnitude;
		float fml = EvaluateFirstDerivative3D(p1,p2,p3,p4,ml).magnitude;
		float fm  = EvaluateFirstDerivative3D(p1,p2,p3,p4,m).magnitude;
		float fmr = EvaluateFirstDerivative3D(p1,p2,p3,p4,mr).magnitude;
		float fmrr= EvaluateFirstDerivative3D(p1,p2,p3,p4,mrr).magnitude;

		// Both the 4-point and 7-point rule integrals are evaluted
		float integral4 = (h/6)*(fa+fb+5*(fml+fmr));
		float integral7 = (h/1470)*(77*(fa+fb)+432*(fmll+fmrr)+625*(fml+fmr)+672*fm);

		// The difference betwen the 4-point and 7-point integrals is the
		// estimate of the accuracy

		if((integral4-integral7) < acc || mll<=a || b<=mrr) 
		{
			if (!(m>a && b>m))
			{
				Debug.LogError("Spline integration reached an interval with no more machine numbers");
			}
			return integral7;
		}else{
			return    GaussLobattoIntegrationStep(p1,p2,p3,p4, a, mll, fa, fmll, nevals, maxevals, acc)  
					+ GaussLobattoIntegrationStep(p1,p2,p3,p4, mll, ml, fmll, fml, nevals, maxevals, acc)
					+ GaussLobattoIntegrationStep(p1,p2,p3,p4, ml, m, fml, fm, nevals, maxevals, acc)
					+ GaussLobattoIntegrationStep(p1,p2,p3,p4, m, mr, fm, fmr, nevals, maxevals, acc)
					+ GaussLobattoIntegrationStep(p1,p2,p3,p4, mr, mrr, fmr, fmrr, nevals, maxevals, acc)
					+ GaussLobattoIntegrationStep(p1,p2,p3,p4, mrr, b, fmrr, fb, nevals, maxevals, acc);
			
		}
	}

	/**
	 * Returns the curve parameter (mu) at a certain length of the curve, using linear interpolation
	 * of the values cached in arcLengthTable.
	 */
	public float GetMuAtLenght(float length){

		if (length <= 0) return 0;
		if (length >= totalSplineLenght) return 1;
		
		int i;
		for (i = 1; i < arcLengthTable.Count; ++i) {
			if (length < arcLengthTable[i]) break; 
		}

		float prevMu = (i-1)/(float)(arcLengthTable.Count-1);
		float nextMu = i/(float)(arcLengthTable.Count-1);

		float s = (length - arcLengthTable[i-1]) / (arcLengthTable[i] - arcLengthTable[i-1]);

		return prevMu + (nextMu - prevMu) * s;
		
	}

	public int GetSpanControlPointForMu(float mu, out float spanMu){

		int spanCount = GetNumSpans();
		spanMu = mu * spanCount;
		int i = (mu >= 1f) ? (spanCount - 1) : (int) spanMu;
		spanMu -= i;

		return i;
	}
	
	/**
	* Returns spline position at time mu, with 0<=mu<=1 where 0 is the start of the spline
	* and 1 is the end.
	*/
	public Vector3 GetPositionAt(float mu){
		
		if (controlPoints.Count >= MinPoints){

			if (!System.Single.IsNaN(mu)){

				float p;
				int i = GetSpanControlPointForMu(mu,out p);
							
				return Evaluate3D(controlPoints[i].position,
				                  controlPoints[i].GetOutTangent(),
				                  controlPoints[(i+1) % controlPoints.Count].GetInTangent(),
				                  controlPoints[(i+1) % controlPoints.Count].position,p);
			}else{
				return controlPoints[0].position;
			}

		}
		//Special case: degenerate spline - point
		else if (controlPoints.Count == 1){ 
			return controlPoints[0].position;
		}else{
			throw new InvalidOperationException("Cannot get position in Catmull-Rom spline because it has zero control points.");
		}
		
	}
	
	/**
	* Returns normal tangent vector at time mu, with 0<=mu<=1 where 0 is the start of the spline
	* and 1 is the end.
	*/
	public Vector3 GetFirstDerivativeAt(float mu){

		if (controlPoints.Count >= MinPoints){

			if (!System.Single.IsNaN(mu)){

				float p;
				int i = GetSpanControlPointForMu(mu,out p);
				
				return EvaluateFirstDerivative3D(controlPoints[i].position,
								                 controlPoints[i].GetOutTangent(),
								                 controlPoints[(i+1) % controlPoints.Count].GetInTangent(),
								                 controlPoints[(i+1) % controlPoints.Count].position,p);
			}else{
				return controlPoints[controlPoints.Count-1].position-controlPoints[0].position;
			}
		}else{
			throw new InvalidOperationException("Cannot get tangent in Catmull-Rom spline because it has zero or one control points.");
		}
	}

	/**
	* Returns acceleration at time mu, with 0<=mu<=1 where 0 is the start of the spline
	* and 1 is the end.
	*/
	public Vector3 GetSecondDerivativeAt(float mu){
		
		if (controlPoints.Count >= MinPoints){
			
			if (!System.Single.IsNaN(mu)){
				
				float p;
				int i = GetSpanControlPointForMu(mu,out p);
				
				return EvaluateSecondDerivative3D(controlPoints[i].position,
								                  controlPoints[i].GetOutTangent(),
								                  controlPoints[(i+1) % controlPoints.Count].GetInTangent(),
								                  controlPoints[(i+1) % controlPoints.Count].position,p);
			}else{
				return Vector3.zero;
			}
		}
		//In all degenerate cases (straight lines or points), acceleration is zero:
		return Vector3.zero;
	}
	
	public Vector3 GetNormalAt(float mu){

		if (controlPoints.Count >= MinPoints){
			
			if (!System.Single.IsNaN(mu)){
				
				float p;
				int i = GetSpanControlPointForMu(mu,out p);

				return Vector3.Slerp(controlPoints[i].normal,controlPoints[(i+1) % controlPoints.Count].normal,p);
				
			}else{
				return Vector3.zero;
			}
		}
		//In all degenerate cases (straight lines or points), acceleration is zero:
		return Vector3.zero;
		
	}	
	
	/**
	* 1D bezier spline interpolation
	*/
	public float Evaluate1D(float y0, float y1, float y2, float y3, float mu){
		
		float imu = 1 - mu;
		return imu * imu * imu * y0 +
			3f * imu * imu * mu * y1 +
			3f * imu * mu * mu * y2 +
			mu * mu * mu * y3;

	}

	/**
	* 1D catmull rom spline second derivative
	*/
	public float EvaluateFirstDerivative1D(float y0, float y1, float y2, float y3, float mu){
		
		float imu = 1 - mu;
		return  3f * imu * imu * (y1 - y0) +
				6f * imu * mu * (y2 - y1) +
				3f * mu * mu * (y3 - y2);

	}

	
	/**
	* 1D catmull rom spline second derivative
	*/
	public float EvaluateSecondDerivative1D(float y0, float y1, float y2, float y3, float mu){
		
		float imu = 1 - mu;
		return  3f * imu * imu * (y1 - y0) +
				6f * imu * mu * (y2 - y1) +
				3f * mu * mu * (y3 - y2);
		
	}
	
	/**
	* 3D spline interpolation
	*/
	public Vector3 Evaluate3D(Vector3 y0, Vector3 y1, Vector3 y2, Vector3 y3, float mu){
		
		return new Vector3(Evaluate1D(y0.x,y1.x,y2.x,y3.x,mu),
			               Evaluate1D(y0.y,y1.y,y2.y,y3.y,mu),
			               Evaluate1D(y0.z,y1.z,y2.z,y3.z,mu));
		
	}

	/**
	* 3D spline first derivative
	*/
	public Vector3 EvaluateFirstDerivative3D(Vector3 y0, Vector3 y1, Vector3 y2, Vector3 y3, float mu){
		
		return new Vector3(EvaluateFirstDerivative1D(y0.x,y1.x,y2.x,y3.x,mu),
		                   EvaluateFirstDerivative1D(y0.y,y1.y,y2.y,y3.y,mu),
		                   EvaluateFirstDerivative1D(y0.z,y1.z,y2.z,y3.z,mu));
		
	}

	/**
	* 3D spline second derivative
	*/
	public Vector3 EvaluateSecondDerivative3D(Vector3 y0, Vector3 y1, Vector3 y2, Vector3 y3, float mu){
		
		return new Vector3(EvaluateSecondDerivative1D(y0.x,y1.x,y2.x,y3.x,mu),
		                   EvaluateSecondDerivative1D(y0.y,y1.y,y2.y,y3.y,mu),
		                   EvaluateSecondDerivative1D(y0.z,y1.z,y2.z,y3.z,mu));
		
	}
	
}
}
