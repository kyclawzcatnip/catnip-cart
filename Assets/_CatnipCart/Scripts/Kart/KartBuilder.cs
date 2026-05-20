using UnityEngine;
using CatnipCart.Core;

namespace CatnipCart.Kart
{
    /// <summary>
    /// Builds a kart from Unity primitives. Procedural chassis, wheels, exhaust pipe.
    /// </summary>
    public class KartBuilder : MonoBehaviour
    {
        public Color primaryColor = new Color(0.96f, 0.62f, 0.04f);
        public Color secondaryColor = Color.white;
        public Color accentColor = new Color(0.96f, 0.45f, 0.71f);

        [HideInInspector] public Transform[] wheelTransforms = new Transform[4];
        [HideInInspector] public Transform bodyTransform;

        void Awake()
        {
            BuildKart();
        }

        void BuildKart()
        {
            Material primaryMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.KartPaint(primaryColor), 0.6f, 0.3f);
            Material secondaryMat = MakeMat(secondaryColor);
            Material accentMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.KartPaint(accentColor), 0.5f, 0.2f);
            Material darkMat = MakeMat(new Color(0.15f, 0.15f, 0.15f));
            Material tireMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.Rubber(), 0.1f);

            // === CHASSIS ===
            bodyTransform = new GameObject("KartBody").transform;
            bodyTransform.SetParent(transform, false);

            // Main body
            var chassis = MakePart("Chassis", PrimitiveType.Cube, primaryMat,
                new Vector3(0, 0.2f, 0), new Vector3(0.7f, 0.18f, 1.2f));
            chassis.transform.SetParent(bodyTransform, false);

            // Hood (slightly raised front)
            var hood = MakePart("Hood", PrimitiveType.Cube, primaryMat,
                new Vector3(0, 0.32f, 0.25f), new Vector3(0.6f, 0.08f, 0.5f));
            hood.transform.SetParent(bodyTransform, false);

            // Rear spoiler
            var spoilerPost1 = MakePart("SpoilerPost1", PrimitiveType.Cylinder, darkMat,
                new Vector3(-0.25f, 0.4f, -0.5f), new Vector3(0.03f, 0.1f, 0.03f));
            spoilerPost1.transform.SetParent(bodyTransform, false);
            var spoilerPost2 = MakePart("SpoilerPost2", PrimitiveType.Cylinder, darkMat,
                new Vector3(0.25f, 0.4f, -0.5f), new Vector3(0.03f, 0.1f, 0.03f));
            spoilerPost2.transform.SetParent(bodyTransform, false);
            var spoilerWing = MakePart("SpoilerWing", PrimitiveType.Cube, accentMat,
                new Vector3(0, 0.52f, -0.5f), new Vector3(0.65f, 0.03f, 0.15f));
            spoilerWing.transform.SetParent(bodyTransform, false);

            // Front bumper
            var bumper = MakePart("FrontBumper", PrimitiveType.Cube, accentMat,
                new Vector3(0, 0.15f, 0.62f), new Vector3(0.75f, 0.1f, 0.08f));
            bumper.transform.SetParent(bodyTransform, false);

            // Seat
            var seat = MakePart("Seat", PrimitiveType.Cube, secondaryMat,
                new Vector3(0, 0.35f, -0.1f), new Vector3(0.35f, 0.15f, 0.3f));
            seat.transform.SetParent(bodyTransform, false);
            var seatBack = MakePart("SeatBack", PrimitiveType.Cube, secondaryMat,
                new Vector3(0, 0.45f, -0.25f), new Vector3(0.35f, 0.2f, 0.06f));
            seatBack.transform.SetParent(bodyTransform, false);

            // Steering wheel
            var steeringCol = MakePart("SteeringColumn", PrimitiveType.Cylinder, darkMat,
                new Vector3(0, 0.38f, 0.15f), new Vector3(0.02f, 0.08f, 0.02f));
            steeringCol.transform.SetParent(bodyTransform, false);
            steeringCol.transform.localRotation = Quaternion.Euler(60, 0, 0);

            // Exhaust pipe
            var exhaust = MakePart("Exhaust", PrimitiveType.Cylinder, darkMat,
                new Vector3(0.2f, 0.2f, -0.65f), new Vector3(0.05f, 0.08f, 0.05f));
            exhaust.transform.SetParent(bodyTransform, false);
            exhaust.transform.localRotation = Quaternion.Euler(90, 0, 0);

            // Fish bone antenna
            var antenna = MakePart("Antenna", PrimitiveType.Cylinder, secondaryMat,
                new Vector3(-0.2f, 0.55f, -0.45f), new Vector3(0.01f, 0.15f, 0.01f));
            antenna.transform.SetParent(bodyTransform, false);
            var fishBone = MakePart("FishBone", PrimitiveType.Sphere, secondaryMat,
                new Vector3(-0.2f, 0.72f, -0.45f), new Vector3(0.05f, 0.02f, 0.08f));
            fishBone.transform.SetParent(bodyTransform, false);

            // === WHEELS ===
            float wheelY = 0.1f;
            float wheelXOff = 0.4f;
            float frontZ = 0.4f, rearZ = -0.4f;
            float wRadius = 0.12f;

            wheelTransforms[0] = MakeWheel("WheelFL", new Vector3(-wheelXOff, wheelY, frontZ), wRadius, tireMat, accentMat);
            wheelTransforms[1] = MakeWheel("WheelFR", new Vector3(wheelXOff, wheelY, frontZ), wRadius, tireMat, accentMat);
            wheelTransforms[2] = MakeWheel("WheelRL", new Vector3(-wheelXOff, wheelY, rearZ), wRadius, tireMat, accentMat);
            wheelTransforms[3] = MakeWheel("WheelRR", new Vector3(wheelXOff, wheelY, rearZ), wRadius, tireMat, accentMat);

            // Remove all colliders from visual parts
            foreach (var col in GetComponentsInChildren<Collider>())
                Destroy(col);
        }

        Transform MakeWheel(string name, Vector3 pos, float radius, Material tireMat, Material hubMat)
        {
            var wheelParent = new GameObject(name).transform;
            wheelParent.SetParent(transform, false);
            wheelParent.localPosition = pos;

            // Tire
            var tire = MakePart(name + "_Tire", PrimitiveType.Cylinder, tireMat,
                Vector3.zero, new Vector3(radius * 2, 0.05f, radius * 2));
            tire.transform.SetParent(wheelParent, false);
            tire.transform.localRotation = Quaternion.Euler(0, 0, 90);

            // Hub
            var hub = MakePart(name + "_Hub", PrimitiveType.Cylinder, hubMat,
                Vector3.zero, new Vector3(radius * 1.2f, 0.055f, radius * 1.2f));
            hub.transform.SetParent(wheelParent, false);
            hub.transform.localRotation = Quaternion.Euler(0, 0, 90);

            return wheelParent;
        }

        GameObject MakePart(string name, PrimitiveType type, Material mat,
            Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        Material MakeMat(Color c)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            mat.SetFloat("_Smoothness", 0.4f);
            return mat;
        }
    }
}
