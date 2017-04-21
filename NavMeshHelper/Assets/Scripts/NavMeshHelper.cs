using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This class represents a tool that helps to prepare the scene to NavMesh baking.
/// It will generate clone objects from colliders already in the scene.
/// These clone objects will contain the renderer generated from the collider shape.
/// Unity NavMesh baking works upon Renderers, so colliders are ignored.
/// This tool fulfil the need of using only colliders for NavMesh baking.
/// Read the instructions for more information.
/// </summary>
public class NavMeshHelper : EditorWindow
{
    /// <summary>
    /// Holds the renderers disabled when setting up the bake mode.
    /// This will be used to go back to the normal mode after baking.
    /// </summary>
    private Stack<Renderer> disableds;

    /// <summary>
    /// Layer mask field, the tool will generate clone objects only within this layer mask.
    /// </summary>
    private LayerMask layerMask = 0;

    /// <summary>
    /// The object that will hold all the clones/fakes as childs.
    /// Bake mode.
    /// </summary>
    private GameObject bakeModeRoot = null;

    /// <summary>
    /// Holds the diffuse material reference.
    /// This is used to generate the MeshCollider's clone/fake.
    /// </summary>
    private Material diffuseDefaultMaterial = null;

    /// <summary>
    /// Mode to change the GUI draw. Bake Mode vs Normal Mode
    /// </summary>
    private bool bakeMode = false;

    [MenuItem("Luigi/Open NavMeshHelper Window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<NavMeshHelper>();
    }

    private void Awake()
    {
        layerMask.value = 1;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("NavMesh Helper Attributes", EditorStyles.boldLabel);
        var oldValue = layerMask.value;
        layerMask = CustomEditorExtension.LayerMaskField("Layer Mask: ", layerMask);
        if (layerMask.value != oldValue)
        {
            BackToNormalMode();
        }

        GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1.0f) });
        EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);

        if (!bakeMode)
        {
            // NORMAL MODE GUI
            if (GUILayout.Button("Go to NavMesh Bake Mode"))
            {
                bakeMode = true;
                SetupNavMeshBakeMode();
            }
        }
        else
        {
            // BAKE MODE GUI
            if (GUILayout.Button("Back to Normal Mode"))
            {
                BackToNormalMode();
            }
        }

        // draw a horizontal line
        GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
    }

    /// <summary>
    /// Setup the Bake Mode.
    /// Hide all the renderers in the scene, and generate all the clone/fake objects.
    /// Keep the original object's static flags and navmesh area setup, and configure it on the clone/fake.
    /// </summary>
    private void SetupNavMeshBakeMode()
    {
        // get the real renderers before generate the new renderers(the fakes)
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        // create the fake object's root
        bakeModeRoot = new GameObject("Bake Mode Root");
        bakeModeRoot.transform.parent = null;

        // generate the fake objects based on the existent colliders
        Collider[] colliders = FindObjectsOfType<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            int theTrueMaskSupose = layerMask.value | 1 << colliders[i].gameObject.layer;
            if (layerMask.value == theTrueMaskSupose && !colliders[i].isTrigger)
            {
                // generate and store the fake object
                GameObject fakeObject = GenerateRendererObject(colliders[i]);

                // in some cases, the fakeObject can be null 
                // for example: it has a MeshCollider but doesnt have a MeshRenderer

                if (fakeObject != null)
                {
                    // setup in the root(all fake objects must be child of this, to make it easy to setup and delete later)
                    fakeObject.transform.parent = bakeModeRoot.transform;

                    // setup layer
                    fakeObject.layer = colliders[i].gameObject.layer;

                    // setup flags
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(colliders[i].gameObject);
                    GameObjectUtility.SetStaticEditorFlags(fakeObject, flags);

                    // setup nav mesh area
                    GameObjectUtility.SetNavMeshArea(fakeObject, GameObjectUtility.GetNavMeshArea(colliders[i].gameObject));
                }
            }
        }

        // disable renderers
        disableds = new Stack<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].enabled)
            {
                renderers[i].enabled = false;
                // keep the disableds reference in a stack, so we can enable it later
                disableds.Push(renderers[i]);
            }
        }
    }

    /// <summary>
    /// Go back to normal mode. (From BakeMode to NormalMode)
    /// Enable all the renderers that were disabled by the BakeMode setup.
    /// Destroy the temp/fake/clone objects.
    /// Go back to the scene's normal setup.
    /// </summary>
    private void BackToNormalMode()
    {
        bakeMode = false;
        // enable objects
        while (disableds != null && disableds.Count > 0)
        {
            disableds.Pop().enabled = true;
        }

        // destroy the temp/fake objects
        if (bakeModeRoot != null)
        {
            DestroyImmediate(bakeModeRoot);
            bakeModeRoot = null;
        }

    }

    /// <summary>
    /// Generate an object with a renderer based on a collider.
    /// The method accepts: BoxCollider, CapsuleCollider, SphereCollider and MeshCollider.
    /// </summary>
    private GameObject GenerateRendererObject(Collider theCollider)
    {
        GameObject fakeObject = null;
        const string DEFAULT_FAKEOBJECT_NAME = "Object";

        if (theCollider.GetType() == typeof(BoxCollider))
        {
            #region In case col is a box
            BoxCollider baseCollider = theCollider as BoxCollider;
            fakeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(fakeObject.GetComponent<Collider>());
            fakeObject.name = DEFAULT_FAKEOBJECT_NAME;
            fakeObject.transform.rotation = theCollider.gameObject.transform.rotation;
            fakeObject.transform.parent = theCollider.gameObject.transform;
            fakeObject.transform.localPosition = baseCollider.center;
            fakeObject.transform.parent = null;
            fakeObject.transform.localScale = theCollider.gameObject.transform.lossyScale;
            Vector3 tempScale = fakeObject.transform.localScale;
            tempScale.x *= baseCollider.size.x;
            tempScale.y *= baseCollider.size.y;
            tempScale.z *= baseCollider.size.z;
            fakeObject.transform.localScale = tempScale;
            #endregion
        }
        else if (theCollider.GetType() == typeof(CapsuleCollider))
        {
            #region In case col is a capsule
            CapsuleCollider baseCollider = theCollider as CapsuleCollider;
            fakeObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            DestroyImmediate(fakeObject.GetComponent<Collider>());
            fakeObject.name = DEFAULT_FAKEOBJECT_NAME;
            fakeObject.transform.rotation = theCollider.gameObject.transform.rotation;
            fakeObject.transform.parent = theCollider.gameObject.transform;
            fakeObject.transform.localPosition = baseCollider.center;
            fakeObject.transform.parent = null;
            fakeObject.transform.localScale = theCollider.gameObject.transform.lossyScale;
            const float DEFAULT_CAPSULE_RADIUS = 0.5f;
            const float DEFAULT_CAPSULE_HEIGHT = 2.0f;
            Vector3 tempScale = fakeObject.transform.localScale;

            // max(x,z) code
            if (Mathf.Abs(tempScale.x) > Mathf.Abs(tempScale.z))
            {
                tempScale.z = tempScale.x;
            }
            else
            {
                tempScale.x = tempScale.z;
            }

            tempScale.x *= baseCollider.radius / DEFAULT_CAPSULE_RADIUS;
            tempScale.z *= baseCollider.radius / DEFAULT_CAPSULE_RADIUS;
            tempScale.y *= baseCollider.height / DEFAULT_CAPSULE_HEIGHT;
            fakeObject.transform.localScale = tempScale;
            #endregion
        }
        else if (theCollider.GetType() == typeof(SphereCollider))
        {
            #region In case col is a sphere
            SphereCollider baseCollider = theCollider as SphereCollider;
            fakeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(fakeObject.GetComponent<Collider>());
            fakeObject.name = DEFAULT_FAKEOBJECT_NAME;
            fakeObject.transform.rotation = theCollider.gameObject.transform.rotation;
            fakeObject.transform.parent = theCollider.gameObject.transform;
            fakeObject.transform.localPosition = baseCollider.center;
            fakeObject.transform.parent = null;
            fakeObject.transform.localScale = theCollider.gameObject.transform.lossyScale;
            const float DEFAULT_SPHERE_RADIUS = 0.5f;
            Vector3 tempScale = fakeObject.transform.localScale;

            // max(x,y,z) code
            if (Mathf.Abs(tempScale.x) > Mathf.Abs(tempScale.y))
            {
                tempScale.y = tempScale.x;
            }
            else
            {
                tempScale.x = tempScale.y;
            }
            if (Mathf.Abs(tempScale.x) > Mathf.Abs(tempScale.z))
            {
                tempScale.z = tempScale.y = tempScale.x;
            }
            else
            {
                tempScale.x = tempScale.y = tempScale.z;
            }

            tempScale.x *= baseCollider.radius / DEFAULT_SPHERE_RADIUS;
            tempScale.y *= baseCollider.radius / DEFAULT_SPHERE_RADIUS;
            tempScale.z *= baseCollider.radius / DEFAULT_SPHERE_RADIUS;
            fakeObject.transform.localScale = tempScale;
            #endregion
        }
        else if (theCollider.GetType() == typeof(MeshCollider))
        {
            #region In case col is a mesh
            MeshCollider baseCollider = theCollider as MeshCollider;
            // to generate the MeshCollider object, the original MUST HAVE a MeshRenderer
            if (baseCollider.GetComponent<MeshRenderer>() != null)
            {
                int materialsCount = baseCollider.GetComponent<MeshRenderer>().sharedMaterials.Length;
                fakeObject = new GameObject(DEFAULT_FAKEOBJECT_NAME);
                fakeObject.transform.position = theCollider.gameObject.transform.position;
                fakeObject.transform.rotation = theCollider.gameObject.transform.rotation;
                fakeObject.AddComponent<MeshFilter>().sharedMesh = baseCollider.sharedMesh;
                if (diffuseDefaultMaterial == null)
                {
                    SetDefaultMaterialReference();
                }
                Material[] mats = new Material[materialsCount];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = diffuseDefaultMaterial;
                }
                fakeObject.AddComponent<MeshRenderer>().materials = mats;
                fakeObject.transform.localScale = theCollider.gameObject.transform.lossyScale;
            }
            #endregion
        }

        return fakeObject;
    }

    /// <summary>
    /// Called by the GenerateRendererObject method.
    /// This is a hack to get the diffuse material and store it.
    /// This material will be used to generate objects from a MeshCollider.
    /// </summary>
    private void SetDefaultMaterialReference()
    {
        GameObject tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        diffuseDefaultMaterial = tempPrimitive.GetComponent<MeshRenderer>().sharedMaterial;
        DestroyImmediate(tempPrimitive);
    }

    private void OnDestroy()
    {
        BackToNormalMode();
    }
}
