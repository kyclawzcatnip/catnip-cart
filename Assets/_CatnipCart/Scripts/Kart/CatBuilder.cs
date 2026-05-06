using UnityEngine;
using CatnipCart.Core;

namespace CatnipCart.Kart
{
    /// <summary>
    /// Builds a cat character from Unity primitives, matching the
    /// Super Cat World art style. Orange tabby with hat by default.
    /// </summary>
    public class CatBuilder : MonoBehaviour
    {
        public CatColorData colorData;
        public bool wearHat = true;

        [Header("Animation")]
        public KartController kart;

        // Generated parts for animation
        private Transform head;
        private Transform leftEar, rightEar;
        private Transform[] tailSegments;
        private Transform hat;
        private Transform leftPupil, rightPupil;
        private Material bodyMat, bellyMat, pawMat, earMat, eyeMat, noseMat, hatMat;

        void Start()
        {
            if (colorData == null) colorData = CatColorData.CreateGinger();
            BuildCat();
        }

        void Update()
        {
            AnimateCat();
        }

        void BuildCat()
        {
            // Create materials
            bodyMat = MakeMat(colorData.body);
            Material bodyDarkMat = MakeMat(colorData.bodyDark);
            bellyMat = MakeMat(colorData.belly);
            pawMat = MakeMat(colorData.paw);
            earMat = MakeMat(colorData.innerEar);
            eyeMat = MakeMat(Color.white);
            Material pupilMat = MakeMat(colorData.eyes);
            noseMat = MakeMat(colorData.nose);
            hatMat = MakeMat(new Color(0.8f, 0.1f, 0.1f)); // Red hat

            // === BODY (capsule) ===
            var body = MakePrimitive("Body", PrimitiveType.Capsule, bodyMat,
                new Vector3(0, 0.45f, 0), new Vector3(0.35f, 0.25f, 0.3f));

            // Belly patch
            MakePrimitive("Belly", PrimitiveType.Sphere, bellyMat,
                new Vector3(0, 0.4f, 0.08f), new Vector3(0.22f, 0.18f, 0.15f));

            // === HEAD ===
            head = MakePrimitive("Head", PrimitiveType.Sphere, bodyMat,
                new Vector3(0, 0.7f, 0.15f), new Vector3(0.32f, 0.28f, 0.3f)).transform;

            // Ears
            leftEar = MakeEar("LeftEar", -0.1f, bodyMat, earMat);
            rightEar = MakeEar("RightEar", 0.1f, bodyMat, earMat);

            // Eyes (white)
            var leftEyeObj = MakePrimitive("LeftEye", PrimitiveType.Sphere, eyeMat,
                new Vector3(-0.07f, 0.73f, 0.28f), new Vector3(0.08f, 0.08f, 0.05f));
            var rightEyeObj = MakePrimitive("RightEye", PrimitiveType.Sphere, eyeMat,
                new Vector3(0.07f, 0.73f, 0.28f), new Vector3(0.08f, 0.08f, 0.05f));

            // Pupils
            leftPupil = MakePrimitive("LeftPupil", PrimitiveType.Sphere, pupilMat,
                new Vector3(-0.07f, 0.73f, 0.31f), new Vector3(0.045f, 0.06f, 0.03f)).transform;
            rightPupil = MakePrimitive("RightPupil", PrimitiveType.Sphere, pupilMat,
                new Vector3(0.07f, 0.73f, 0.31f), new Vector3(0.045f, 0.06f, 0.03f)).transform;

            // Nose
            MakePrimitive("Nose", PrimitiveType.Sphere, noseMat,
                new Vector3(0, 0.68f, 0.3f), new Vector3(0.04f, 0.03f, 0.03f));

            // Whiskers (thin cylinders)
            for (int side = -1; side <= 1; side += 2)
            {
                for (int w = -1; w <= 1; w++)
                {
                    var whisker = MakePrimitive($"Whisker_{side}_{w}", PrimitiveType.Cylinder,
                        bodyDarkMat, Vector3.zero, new Vector3(0.008f, 0.08f, 0.008f));
                    whisker.transform.SetParent(head, false);
                    whisker.transform.localPosition = new Vector3(side * 0.12f, -0.02f + w * 0.03f, 0.35f);
                    whisker.transform.localRotation = Quaternion.Euler(0, 0, side * (70 + w * 15));
                }
            }

            // === LEGS (4 small cylinders) ===
            float legY = 0.15f;
            MakePrimitive("LegFL", PrimitiveType.Cylinder, bodyDarkMat,
                new Vector3(-0.1f, legY, 0.08f), new Vector3(0.06f, 0.12f, 0.06f));
            MakePrimitive("LegFR", PrimitiveType.Cylinder, bodyDarkMat,
                new Vector3(0.1f, legY, 0.08f), new Vector3(0.06f, 0.12f, 0.06f));
            MakePrimitive("LegBL", PrimitiveType.Cylinder, bodyDarkMat,
                new Vector3(-0.1f, legY, -0.08f), new Vector3(0.06f, 0.12f, 0.06f));
            MakePrimitive("LegBR", PrimitiveType.Cylinder, bodyDarkMat,
                new Vector3(0.1f, legY, -0.08f), new Vector3(0.06f, 0.12f, 0.06f));

            // Paws
            MakePrimitive("PawFL", PrimitiveType.Sphere, pawMat,
                new Vector3(-0.1f, 0.04f, 0.08f), new Vector3(0.07f, 0.04f, 0.07f));
            MakePrimitive("PawFR", PrimitiveType.Sphere, pawMat,
                new Vector3(0.1f, 0.04f, 0.08f), new Vector3(0.07f, 0.04f, 0.07f));
            MakePrimitive("PawBL", PrimitiveType.Sphere, pawMat,
                new Vector3(-0.1f, 0.04f, -0.08f), new Vector3(0.07f, 0.04f, 0.07f));
            MakePrimitive("PawBR", PrimitiveType.Sphere, pawMat,
                new Vector3(0.1f, 0.04f, -0.08f), new Vector3(0.07f, 0.04f, 0.07f));

            // === TAIL ===
            BuildTail(bodyMat);

            // === HAT ===
            if (wearHat)
            {
                hat = new GameObject("Hat").transform;
                hat.SetParent(head, false);
                hat.localPosition = new Vector3(0, 0.18f, 0);

                // Hat brim (flattened cylinder)
                var brim = MakePrimitive("HatBrim", PrimitiveType.Cylinder, hatMat,
                    Vector3.zero, new Vector3(0.2f, 0.015f, 0.2f));
                brim.transform.SetParent(hat, false);

                // Hat top (cylinder)
                var top = MakePrimitive("HatTop", PrimitiveType.Cylinder, hatMat,
                    new Vector3(0, 0.06f, 0), new Vector3(0.13f, 0.06f, 0.13f));
                top.transform.SetParent(hat, false);
            }

            // Remove colliders from all child parts (kart has its own collider)
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (col.gameObject != gameObject)
                    Destroy(col);
            }
        }

        void BuildTail(Material mat)
        {
            tailSegments = new Transform[6];
            for (int i = 0; i < tailSegments.Length; i++)
            {
                float t = i / (float)(tailSegments.Length - 1);
                var seg = MakePrimitive($"Tail_{i}", PrimitiveType.Sphere, mat,
                    new Vector3(0, 0.45f + t * 0.25f, -0.18f - t * 0.2f),
                    Vector3.one * (0.06f - t * 0.02f));
                tailSegments[i] = seg.transform;
            }
        }

        Transform MakeEar(string name, float xOff, Material outerMat, Material innerMat)
        {
            var earParent = new GameObject(name).transform;
            earParent.SetParent(head, false);
            earParent.localPosition = new Vector3(xOff, 0.18f, 0);

            // Outer ear (scaled cube to look like a triangle)
            var outer = MakePrimitive(name + "_Outer", PrimitiveType.Cube, outerMat,
                Vector3.zero, new Vector3(0.06f, 0.1f, 0.04f));
            outer.transform.SetParent(earParent, false);
            outer.transform.localRotation = Quaternion.Euler(0, 0, xOff > 0 ? -15 : 15);

            // Inner ear
            var inner = MakePrimitive(name + "_Inner", PrimitiveType.Cube, innerMat,
                new Vector3(0, 0.01f, 0.01f), new Vector3(0.04f, 0.07f, 0.02f));
            inner.transform.SetParent(earParent, false);
            inner.transform.localRotation = Quaternion.Euler(0, 0, xOff > 0 ? -15 : 15);

            return earParent;
        }

        GameObject MakePrimitive(string name, PrimitiveType type, Material mat,
            Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        Material MakeMat(Color c)
        {
            // Use URP Lit shader
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            mat.SetFloat("_Smoothness", 0.3f);
            return mat;
        }

        // === ANIMATION ===
        void AnimateCat()
        {
            if (kart == null) return;
            float t = Time.time;

            // Head turns toward steering
            if (head != null)
            {
                float steerLook = kart.CurrentState == KartController.KartState.Drifting
                    ? kart.DriftDirection * 25f
                    : 0f;
                Quaternion targetRot = Quaternion.Euler(0, steerLook, 0);
                head.localRotation = Quaternion.Slerp(head.localRotation, targetRot, 5f * Time.deltaTime);
            }

            // Pupils look toward turns
            if (leftPupil != null && rightPupil != null)
            {
                float lookX = kart.NormalizedSpeed > 0.1f ? 0.01f : 0f;
                leftPupil.localPosition = new Vector3(-0.07f + lookX, 0.73f, 0.31f);
                rightPupil.localPosition = new Vector3(0.07f + lookX, 0.73f, 0.31f);
            }

            // Ears flatten when boosting
            if (leftEar != null && rightEar != null)
            {
                float earAngle = kart.IsBoosting ? 45f : 0f;
                leftEar.localRotation = Quaternion.Slerp(leftEar.localRotation,
                    Quaternion.Euler(earAngle, 0, 0), 8f * Time.deltaTime);
                rightEar.localRotation = Quaternion.Slerp(rightEar.localRotation,
                    Quaternion.Euler(earAngle, 0, 0), 8f * Time.deltaTime);
            }

            // Tail sways
            if (tailSegments != null)
            {
                bool drifting = kart.CurrentState == KartController.KartState.Drifting;
                for (int i = 0; i < tailSegments.Length; i++)
                {
                    if (tailSegments[i] == null) continue;
                    float seg_t = i / (float)(tailSegments.Length - 1);
                    float sway = drifting ? 0f : Mathf.Sin(t * 3f + i * 0.5f) * 0.03f * seg_t;
                    var pos = tailSegments[i].localPosition;
                    pos.x = sway;
                    tailSegments[i].localPosition = pos;
                }
            }
        }
    }
}
