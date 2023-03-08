using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching
{
    using TrajectoryFeature = MotionMatchingData.TrajectoryFeature;

    public class RVO_Character_Controller : MotionMatchingCharacterController
    {
        public string TrajectoryPositionFeatureName = "FuturePosition";
        public string TrajectoryDirectionFeatureName = "FutureDirection";
        public KeyPoint[] Path; // The key points of the path. Guardan Pos(x,z), Velocidad, y una función

        private int CurrentKeyPoint; // The current key point index
        private float CurrentKeyPointT; // [0..1] which part of the current keypoint is the character currently at

        private float2 CurrentPosition;
        private float2 CurrentDirection;
        private float2[] PredictedPositions;
        private float2[] PredictedDirections;

        // Features -----------------------------------------------------------------
        private int TrajectoryPosFeatureIndex;
        private int TrajectoryRotFeatureIndex;
        private int[] TrajectoryPosPredictionFrames;
        private int[] TrajectoryRotPredictionFrames;
        private int NumberPredictionPos { get { return TrajectoryPosPredictionFrames.Length; } }
        private int NumberPredictionRot { get { return TrajectoryRotPredictionFrames.Length; } }
        // --------------------------------------------------------------------------

        GameObject Joe;
        Agent JoeAgent;

        public float x_ini, z_ini;
        public float x_goal, z_goal;

        private void Awake()
        {
            Debug.Log("INICIALIZO A JOE Y A SU AGENTE EN EL CONTROLLER");
            Joe = gameObject.transform.parent.GetChild(2).gameObject;
            JoeAgent = Joe.GetComponent<Agent>();
        }
        private void Start()
        {
            //Get the feature indices

            TrajectoryPosFeatureIndex = -1;
            TrajectoryRotFeatureIndex = -1;
            for (int i = 0; i < SimulationBone.MMData.TrajectoryFeatures.Count; ++i) //Solo tiene 2 unidades (FuturaPosition y FutureDirection
            {
                if (SimulationBone.MMData.TrajectoryFeatures[i].Name == TrajectoryPositionFeatureName) TrajectoryPosFeatureIndex = i; //Siempre es 0
                if (SimulationBone.MMData.TrajectoryFeatures[i].Name == TrajectoryDirectionFeatureName) TrajectoryRotFeatureIndex = i; //Siempre es 1
            }

            Debug.Assert(TrajectoryPosFeatureIndex != -1, "Trajectory Position Feature not found");
            Debug.Assert(TrajectoryRotFeatureIndex != -1, "Trajectory Direction Feature not found");

            TrajectoryPosPredictionFrames = SimulationBone.MMData.TrajectoryFeatures[TrajectoryPosFeatureIndex].FramesPrediction; //Vectores
            TrajectoryRotPredictionFrames = SimulationBone.MMData.TrajectoryFeatures[TrajectoryRotFeatureIndex].FramesPrediction; //Vectores
            // TODO: generalize this, allow for different number of prediction frames
            Debug.Assert(TrajectoryPosPredictionFrames.Length == TrajectoryRotPredictionFrames.Length, "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            for (int i = 0; i < TrajectoryPosPredictionFrames.Length; ++i)
            {
                Debug.Assert(TrajectoryPosPredictionFrames[i] == TrajectoryRotPredictionFrames[i], "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            }

            PredictedPositions = new float2[NumberPredictionPos]; //TrajectoryPosPredictionFrames.Length;
            PredictedDirections = new float2[NumberPredictionRot]; //Aquí basicamente se guardará todas las posiciones y direcciones predichas
        }
        
        protected override void OnUpdate() 
        {
            // Predict the future positions and directions
            for (int i = 0; i < NumberPredictionPos; i++)
            { //AveragedDeltaTime = FrameTime o 20, una de dos;
                SimulatePath(AveragedDeltaTime * TrajectoryPosPredictionFrames[i], CurrentKeyPoint, CurrentKeyPointT, //FrameTime*PosFrame, 
                             out _, out _,
                             out PredictedPositions[i], out PredictedDirections[i]);
            }
            // Update Current Position and Direction
            SimulatePath(Time.deltaTime, CurrentKeyPoint, CurrentKeyPointT,
                         out CurrentKeyPoint, out CurrentKeyPointT,
                         out CurrentPosition, out CurrentDirection);
        }
        
        private void SimulatePath(float remainingTime, int currentKeypoint, float currentKeyPointT,
                              out int nextKeypoint, out float nextKeyPointTime,
                              out float2 nextPos, out float2 nextDir)
            {
            // Just in case remainingTime is negative or 0
            nextPos = float2.zero;
            nextDir = float2.zero;
            if (remainingTime <= 0)
            {
                KeyPoint current = Path[currentKeypoint];
                KeyPoint next = Path[(currentKeypoint + 1) % Path.Length];
                float2 dir = next.Position - current.Position;
                nextPos = current.Position + dir * currentKeyPointT;
                nextDir = math.normalize(dir);
            }
            // Loop until the character has moved enough
            while (remainingTime > 0)
            {
                KeyPoint current = Path[currentKeypoint]; //POS INICIAL
                KeyPoint next = Path[(currentKeypoint + 1) % Path.Length]; //GOAL
                float2 dir = next.Position - current.Position; //DIRECCION = GOAL - POS INICIAL
                float2 dirNorm = math.normalize(dir); //DIRECCION NORMALIZADA
                float2 currentPos = current.Position + dir * currentKeyPointT;//CURRENT POS
                float timeToNext = math.distance(currentPos, next.Position) / current.Velocity; // Time needed to get to the next keypoint
                float dt = math.min(remainingTime, timeToNext);
                remainingTime -= dt;
                if (remainingTime <= 0)
                {
            Debug.Log("current.Velocity = " + current.Velocity);
            // Move
                    currentPos += dirNorm * current.Velocity * dt;
                Debug.Log("Path current pose = " + currentPos);

                    currentKeyPointT = math.distance(current.Position, currentPos) / math.distance(current.Position, next.Position);
                    nextPos = currentPos;
                    nextDir = dirNorm;
                }
                else
                {
                    // Advance to next keypoint
                    currentKeypoint = (currentKeypoint + 1) % Path.Length;
                    currentKeyPointT = 0;
                }
            }
            Debug.Assert(math.abs(remainingTime) < 0.0001f, "Character did not move enough or moved to much. remainingTime = " + remainingTime);

            nextKeypoint = currentKeypoint;
            nextKeyPointTime = currentKeyPointT;
        }


        public float3 GetCurrentPosition()
        {
            return transform.position + new Vector3(CurrentPosition.x, 0, CurrentPosition.y);
        }

        public quaternion GetCurrentRotation()
        {
            Quaternion rot = Quaternion.LookRotation(new Vector3(CurrentDirection.x, 0, CurrentDirection.y));
            return rot * transform.rotation;
        }

        public override void GetTrajectoryFeature(TrajectoryFeature feature, int index, Transform character, NativeArray<float> output)
        {
            if (!feature.SimulationBone) Debug.Assert(false, "Trajectory should be computed using the SimulationBone");
            switch (feature.FeatureType)
            {
                case TrajectoryFeature.Type.Position:
                    float2 world = GetWorldPredictedPos(index);
                    float3 local = character.InverseTransformPoint(new float3(world.x, 0.0f, world.y));
                    output[0] = local.x;
                    output[1] = local.z;
                    break;
                case TrajectoryFeature.Type.Direction:
                    float2 worldDir = GetWorldPredictedDir(index);
                    float3 localDir = character.InverseTransformDirection(new Vector3(worldDir.x, 0.0f, worldDir.y));
                    output[0] = localDir.x;
                    output[1] = localDir.z;
                    break;
                default:
                    Debug.Assert(false, "Unknown feature type: " + feature.FeatureType);
                    break;
            }
        }

        private float2 GetWorldPredictedPos(int index)
        {
            Debug.Log("HAGO EL GETWORLDPREDICTED POSITIONS");

            return Option1(index);
         // return Option2(index); //Original

        }

        private float2 Option1(int index)
        {
            Debug.Log("INICIAR PREDECIR POSICION = " + index);
            Debug.Log("index = " + index);

            //Necesito: Pos Actual, DirNorm, velocidad y dt.

            Rigidbody rb = Joe.GetComponent<Rigidbody>();

            //PosActual:
            float2 Joe_CurrentPose = new float2(Joe.transform.position.x, Joe.transform.position.z); //LA POS_ACTUAL ES LA DE JOE O LA DE SU RIGIDBODY? (LIGERAMENTE DISTINTA)
            // Vector2 Joe_CurrentPose = new Vector2(rb.position.x, rb.position.z);

            //DirNorm:
            float2 dir = new float2(x_goal, z_goal) - Joe_CurrentPose;
            float2 dirNorm = math.normalize(dir);

            //Velocidad :
            int Joe_id = JoeAgent.GetID();
            Vector3 v_Joe = new Vector3(RVO.Simulator.Instance.getAgentVelocity(Joe_id).x_, 0.0f, RVO.Simulator.Instance.getAgentVelocity(Joe_id).y_);
            float Joe_velocity = rb.velocity.magnitude;
            Debug.Log("velocidad en GetWorldPosPred = " + v_Joe);

            //dt (dt = remainingTime?) (remainingTime = AveragedDeltaTime * frames)
            float frames = TrajectoryPosPredictionFrames[index];
            float remainingTime = AveragedDeltaTime * frames;

            Debug.Log("Con " + TrajectoryPosPredictionFrames[index] + " frames");
            Debug.Log("remaining Time = " + remainingTime);
            Debug.Log("v_joe * frames = " + v_Joe * frames);

           //Calcular
            var aux = dirNorm * (new float2(v_Joe.x, v_Joe.z)) * remainingTime; // = dirNorm*Vel*dt, pero, Vel de rigidbody
            float2 Joe_NextPose = (Joe_CurrentPose + aux);
            Debug.Log("Joe current Pose = " + rb.position);
            Debug.Log("Joe next pose = " + Joe_NextPose);

            Debug.Log("FINALIZAR PREDECIR POSICION");

            //Prueba de otra fórmula con el RigidBody
            Vector3 PredPos2 = rb.position + ( v_Joe * remainingTime);
            float2 PredPos2_float2 = new float2(PredPos2.x, PredPos2.z);

            return Joe_NextPose;// + new float2(transform.position.x, transform.position.z);
        }

        private float2 Option2(int index)
        {
            return PredictedPositions[index] + new float2(transform.position.x, transform.position.z);
        }

        private float2 GetWorldPredictedDir(int index)
        {
            Debug.Log("HAGO EL GETWORLDPREDICTED DIRECTIONS");

            int Joe_id = JoeAgent.GetID();
            Vector3 v_Joe = new Vector3(RVO.Simulator.Instance.getAgentVelocity(Joe_id).x_, 0.0f, RVO.Simulator.Instance.getAgentVelocity(Joe_id).y_);

            Vector2 PosActualDeJoe = new Vector2(Joe.transform.position.x, Joe.transform.position.z);

            float2 dir = new Vector2(x_goal, z_goal) - PosActualDeJoe;

            Debug.Log("EN PREDICTABLE: Si mi goal es " + new Vector2(x_goal, z_goal) + " y mi pos actual es " + PosActualDeJoe + " entonces mi dirección es " + dir);

            return math.normalize(dir);
            return PredictedDirections[index];
        }

      
        public override float3 GetWorldInitPosition()
        {
            //CRO LAS COORDENADAS DE JOE
            GameObject CrowdCreator = GameObject.FindGameObjectWithTag("CrowdCreatorObject");
            RVO_Crowd_Generator my_rvo = CrowdCreator.GetComponent<RVO_Crowd_Generator>();

            float maxX, maxY, radius;
            maxX = my_rvo.maxX;
            maxY = my_rvo.maxY;
            radius = my_rvo.radius;
            float x = UnityEngine.Random.Range(-(maxX - radius), -(maxX - 5f));
            float z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius));

            Vector3 MyInitialCoordinates = new Vector3(x, 0, z);
            Debug.Log("My initial coordinates GWIP = " + MyInitialCoordinates);

            Joe = gameObject.transform.parent.GetChild(2).gameObject;
            JoeAgent = Joe.GetComponent<Agent>();
            Joe.transform.position = MyInitialCoordinates;

            //Las hago accesibles para el CrowdController
            x_ini = x;
            z_ini = z;

            return new float3(Joe.transform.position.x, 0, Joe.transform.position.z);
           // return new float3(Path[0].Position.x, 0, Path[0].Position.y) + (float3)transform.position;
        }
        public override float3 GetWorldInitDirection()
        {
            //PILLO EL GOAL DE JOE
            GameObject CrowdCreator = GameObject.FindGameObjectWithTag("CrowdCreatorObject");
            RVO_Crowd_Generator my_rvo = CrowdCreator.GetComponent<RVO_Crowd_Generator>();

            Debug.Log("Voy a mirar la direccion inicial");

            float2 dir = my_rvo.agent_goal - new Vector2(x_ini, z_ini);

            //Me guardo las coordenadas del Goal
            x_goal = my_rvo.agent_goal.x;
            z_goal = my_rvo.agent_goal.y;

            Debug.Log("Si mi goal es " + my_rvo.agent_goal + " y mi pos_ini es " + new Vector2(x_ini, z_ini) + " entonces mi dirección es " + dir);
            //float2 dir = Path.Length > 0 ? Path[1].Position - Path[0].Position : new float2(0, 1); //if(length>0) then P1 - P2; else new float(0,1)
            return math.normalize(new float3(dir.x, 0, dir.y));
        }

        [System.Serializable]
        public struct KeyPoint
        {
            public float2 Position;
            public float Velocity;

            public float3 GetWorldPosition(Transform transform)
            {
                return transform.position + new Vector3(Position.x, 0, Position.y);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Path == null) return;

            const float heightOffset = 0.01f;

            // Draw KeyPoints
            Gizmos.color = Color.red;
            for (int i = 0; i < Path.Length; i++)
            {
                float3 pos = Path[i].GetWorldPosition(transform);
                Gizmos.DrawSphere(new Vector3(pos.x, heightOffset, pos.z), 0.1f);
            }
            // Draw Path
            Gizmos.color = new Color(0.5f, 0.0f, 0.0f, 1.0f);
            for (int i = 0; i < Path.Length - 1; i++)
            {
                float3 pos = Path[i].GetWorldPosition(transform);
                float3 nextPos = Path[i + 1].GetWorldPosition(transform);
                GizmosExtensions.DrawLine(new Vector3(pos.x, heightOffset, pos.z), new Vector3(nextPos.x, heightOffset, nextPos.z), 6);
            }
            // Last Line
            float3 lastPos = Path[Path.Length - 1].GetWorldPosition(transform);
            float3 firstPos = Path[0].GetWorldPosition(transform);
            GizmosExtensions.DrawLine(new Vector3(lastPos.x, heightOffset, lastPos.z), new Vector3(firstPos.x, heightOffset, firstPos.z), 6);
            // Draw Velocity
            for (int i = 0; i < Path.Length - 1; i++)
            {
                float3 pos = Path[i].GetWorldPosition(transform);
                float3 nextPos = Path[i + 1].GetWorldPosition(transform);
                Vector3 start = new Vector3(pos.x, heightOffset, pos.z);
                Vector3 end = new Vector3(nextPos.x, heightOffset, nextPos.z);
                GizmosExtensions.DrawArrow(start, start + (end - start).normalized * math.min(Path[i].Velocity, math.distance(pos, nextPos)), thickness: 6);
            }
            // Last Line
            float3 lastPos2 = Path[Path.Length - 1].GetWorldPosition(transform);
            float3 firstPos2 = Path[0].GetWorldPosition(transform);
            Vector3 start2 = new Vector3(lastPos2.x, heightOffset, lastPos2.z);
            Vector3 end2 = new Vector3(firstPos2.x, heightOffset, firstPos2.z);
            GizmosExtensions.DrawArrow(start2, start2 + (end2 - start2).normalized * Path[Path.Length - 1].Velocity, thickness: 3);

            // Draw Current Position And Direction
            if (!Application.isPlaying) return;
            Gizmos.color = new Color(1.0f, 0.3f, 0.1f, 1.0f);
            Vector3 currentPos = (Vector3)GetCurrentPosition() + Vector3.up * heightOffset * 2;
            Gizmos.DrawSphere(currentPos, 0.1f);
            GizmosExtensions.DrawLine(currentPos, currentPos + (Quaternion)GetCurrentRotation() * Vector3.forward, 12);
            // Draw Prediction
            if (PredictedPositions == null || PredictedPositions.Length != NumberPredictionPos ||
                PredictedDirections == null || PredictedDirections.Length != NumberPredictionRot) return;
            Gizmos.color = new Color(0.6f, 0.3f, 0.8f, 1.0f);
            for (int i = 0; i < NumberPredictionPos; i++)
            {
                float2 predictedPosf2 = GetWorldPredictedPos(i);
                Vector3 predictedPos = new Vector3(predictedPosf2.x, heightOffset * 2, predictedPosf2.y);
                Gizmos.DrawSphere(predictedPos, 0.1f);
                float2 dirf2 = GetWorldPredictedDir(i);
                GizmosExtensions.DrawLine(predictedPos, predictedPos + new Vector3(dirf2.x, 0.0f, dirf2.y) * 0.5f, 12);
            }
        }

        //No es necesaria
        public void InstantiateAgents()
        {
            Debug.Log("Hola");
            GameObject CrowdCreator = GameObject.FindGameObjectWithTag("CrowdCreatorObject");
            RVO_Crowd_Generator my_rvo = CrowdCreator.GetComponent<RVO_Crowd_Generator>();

            float maxX, maxY, radius;
            maxX = my_rvo.maxX;
            maxY = my_rvo.maxY;
            radius = my_rvo.radius;

            float x = UnityEngine.Random.Range(-(maxX - radius), -(maxX - 5f));
            float z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius));

            Vector3 MyInitialCoordinates = new Vector3(x, 0, z);

            Debug.Log("My Init pos = " + MyInitialCoordinates);

            int agentID = RVO.Simulator.Instance.addAgent(new RVO.Vector2(x, z));

            JoeAgent.SetID(agentID);
            JoeAgent.SetMaximumDiameter(maxX, maxY);
            JoeAgent.SetInitialCoordinates(x, z);
            //JoeAgent.especial(SpecialMat);
            // JoeAgent.SetEscenarioAgent();
            //AddAgent(newAgent);
        }
#endif
    }
}