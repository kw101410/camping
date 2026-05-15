using UnityEngine;
using UnityEditor;

public class CamperSetupTool
{
    [MenuItem("Antigravity/캠핑카 자동 셋업하기")]
    public static void SetupCamper()
    {
        // 1. 바닥 생성
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10, 1, 10);

        // 2. 캠핑카 메인 뼈대 생성
        GameObject camperVan = new GameObject("CamperVan");
        Rigidbody rb = camperVan.AddComponent<Rigidbody>();
        rb.mass = 1500f;
        
        CamperController controller = camperVan.AddComponent<CamperController>();

        // 3. 차체(Body) 시각/충돌 모델 생성 -> 속이 빈 프레임 형태로 변경하여 내부 공간 확보
        GameObject bodyRoot = new GameObject("BodyFrame");
        bodyRoot.transform.SetParent(camperVan.transform);
        bodyRoot.transform.localPosition = Vector3.zero;

        CreateBlock("Floor", new Vector3(0, 0.6f, 0), new Vector3(2.2f, 0.2f, 4.2f), Vector3.zero, bodyRoot.transform);
        CreateBlock("Roof", new Vector3(0, 2.4f, 0), new Vector3(2.2f, 0.2f, 4.2f), Vector3.zero, bodyRoot.transform);
        CreateBlock("Wall_Left", new Vector3(-1f, 1.5f, 0), new Vector3(0.2f, 1.6f, 4.2f), Vector3.zero, bodyRoot.transform);
        CreateBlock("Wall_Right", new Vector3(1f, 1.5f, 0), new Vector3(0.2f, 1.6f, 4.2f), Vector3.zero, bodyRoot.transform);
        CreateBlock("Wall_Front", new Vector3(0, 1.5f, 2f), new Vector3(1.8f, 1.6f, 0.2f), Vector3.zero, bodyRoot.transform);

        // 조수석 플레이어 시각 모델 (빨간 캡슐)
        GameObject passenger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        passenger.name = "Passenger(Player2)";
        passenger.transform.SetParent(camperVan.transform);
        passenger.transform.localPosition = new Vector3(0, 1.1f, 0.5f);
        passenger.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
        
        Material redMat = new Material(Shader.Find("Standard"));
        redMat.color = Color.red;
        passenger.GetComponent<Renderer>().material = redMat;

        controller.passengerModel = passenger.transform;

        // 4. 바퀴 부모 오브젝트 생성
        GameObject wheelsParent = new GameObject("Wheels");
        wheelsParent.transform.SetParent(camperVan.transform);
        wheelsParent.transform.localPosition = Vector3.zero;

        GameObject modelsParent = new GameObject("WheelModels");
        modelsParent.transform.SetParent(camperVan.transform);
        modelsParent.transform.localPosition = Vector3.zero;

        // 5. 4개의 바퀴(콜라이더 및 시각 모델) 생성 및 연결
        Vector3 flPos = new Vector3(-1f, 0.5f, 1.5f);
        Vector3 frPos = new Vector3(1f, 0.5f, 1.5f);
        Vector3 rlPos = new Vector3(-1f, 0.5f, -1.5f);
        Vector3 rrPos = new Vector3(1f, 0.5f, -1.5f);

        controller.frontLeftWheel = CreateWheelCollider("FrontLeft", flPos, wheelsParent.transform);
        controller.frontRightWheel = CreateWheelCollider("FrontRight", frPos, wheelsParent.transform);
        controller.rearLeftWheel = CreateWheelCollider("RearLeft", rlPos, wheelsParent.transform);
        controller.rearRightWheel = CreateWheelCollider("RearRight", rrPos, wheelsParent.transform);

        controller.frontLeftTransform = CreateWheelModel("Mesh_FL", flPos, modelsParent.transform);
        controller.frontRightTransform = CreateWheelModel("Mesh_FR", frPos, modelsParent.transform);
        controller.rearLeftTransform = CreateWheelModel("Mesh_RL", rlPos, modelsParent.transform);
        controller.rearRightTransform = CreateWheelModel("Mesh_RR", rrPos, modelsParent.transform);

        // 에디터 뷰포트를 생성된 캠핑카로 이동
        Selection.activeGameObject = camperVan;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("[Antigravity] 캠핑카 자동 셋업이 완료되었습니다! 플레이 버튼을 눌러보세요.");
    }

    [MenuItem("Antigravity/울퉁불퉁한 터레인(Terrain) 자동 깔기")]
    public static void CreateBumpyTerrain()
    {
        // 1. 기존에 만든 평평한 바닥(Plane)이 있다면 삭제
        GameObject oldGround = GameObject.Find("Ground");
        if (oldGround != null) Object.DestroyImmediate(oldGround);

        // 2. 터레인 데이터 설정
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = 513; // 유니티 터레인 권장 해상도
        terrainData.size = new Vector3(500, 50, 500); // 가로세로 500m, 최대 높이 50m

        // 3. 펄린 노이즈를 이용해 높낮이 맵(Heightmap) 억까스럽게 굴곡지게 만들기
        float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                // 약간의 굴곡을 주기 위한 계산 (스케일 조절)
                float xCoord = (float)x / terrainData.heightmapResolution * 20f;
                float yCoord = (float)y / terrainData.heightmapResolution * 20f;
                
                // 울퉁불퉁한 요철 높이 지정 (최대 높이 50m의 약 10% = 최대 5m 높이의 언덕들)
                heights[y, x] = Mathf.PerlinNoise(xCoord, yCoord) * 0.1f; 
            }
        }
        terrainData.SetHeights(0, 0, heights);

        // 4. 터레인 오브젝트를 씬에 생성
        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        // 터레인의 중심부가 대략 (0,0,0)에 오도록 이동
        terrainObj.transform.position = new Vector3(-250, 0, -250); 

        // 5. 캠핑카가 터레인 밑에 묻히지 않도록 위로 끌어올리기
        GameObject camper = GameObject.Find("CamperVan");
        if (camper != null)
        {
            camper.transform.position = new Vector3(0, 10f, 0); 
            camper.transform.rotation = Quaternion.identity;
            Rigidbody rb = camper.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        Debug.Log("🏔️ [Antigravity] 요철이 가득한 물리 테스트용 터레인이 생성되었습니다!");
    }

    [MenuItem("Antigravity/지옥의 장애물 코스 (경사, 좁은길, 물) 만들기")]
    public static void CreateObstacleCourse()
    {
        // 1. 기존 지형 삭제
        DestroyIfExists("Ground");
        DestroyIfExists("Terrain");
        DestroyIfExists("ObstacleCourse");

        GameObject courseRoot = new GameObject("ObstacleCourse");

        // 2. 스타팅 포인트
        CreateBlock("StartPlatform", new Vector3(0, 0, 0), new Vector3(10, 1, 10), Vector3.zero, courseRoot.transform);

        // 3. 오르막 경사로 (가파름)
        CreateBlock("UpHill", new Vector3(0, 3.85f, 19.5f), new Vector3(10, 1, 30), new Vector3(-15, 0, 0), courseRoot.transform);

        // 4. 중간 휴식처 (낭떠러지 앞)
        CreateBlock("Plateau", new Vector3(0, 7.7f, 39f), new Vector3(10, 1, 10), Vector3.zero, courseRoot.transform);

        // 5. 매우 좁은 다리 (차 폭이 2인데 다리가 2.8이라 아슬아슬함)
        CreateBlock("NarrowBridge", new Vector3(0, 7.7f, 69f), new Vector3(2.8f, 1, 50), Vector3.zero, courseRoot.transform);

        // 6. 착지 지점
        CreateBlock("Landing", new Vector3(0, 7.7f, 99f), new Vector3(10, 1, 10), Vector3.zero, courseRoot.transform);

        // 7. 내리막 경사로
        CreateBlock("DownHill", new Vector3(0, 3.85f, 118.5f), new Vector3(10, 1, 30), new Vector3(15, 0, 0), courseRoot.transform);

        // 8. 도착 지점
        CreateBlock("EndPlatform", new Vector3(0, 0, 138f), new Vector3(20, 1, 20), Vector3.zero, courseRoot.transform);

        // 9. 낭떠러지 아래의 물 (파란색 Plane)
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water_Cliff";
        water.transform.SetParent(courseRoot.transform);
        water.transform.position = new Vector3(0, -15f, 70f);
        water.transform.localScale = new Vector3(20, 1, 20); // 200x200 크기

        Renderer waterRenderer = water.GetComponent<Renderer>();
        if (waterRenderer != null)
        {
            Material waterMat = new Material(Shader.Find("Standard"));
            waterMat.color = new Color(0.2f, 0.5f, 1f, 0.8f);
            
            // Material 렌더 모드를 Transparent로 변경
            waterMat.SetFloat("_Mode", 3);
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.DisableKeyword("_ALPHATEST_ON");
            waterMat.DisableKeyword("_ALPHABLEND_ON");
            waterMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMat.renderQueue = 3000;
            
            waterRenderer.material = waterMat;
        }

        // 콜라이더를 Trigger로 만들어 물에 빠졌을 때 감지할 수 있도록 세팅
        Collider waterCol = water.GetComponent<Collider>();
        if (waterCol != null) waterCol.isTrigger = true;

        // 10. 캠핑카 시작 지점으로 리셋
        GameObject camper = GameObject.Find("CamperVan");
        if (camper != null)
        {
            camper.transform.position = new Vector3(0, 3f, 0);
            camper.transform.rotation = Quaternion.identity;
            Rigidbody rb = camper.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        Debug.Log("💀 [Antigravity] 지옥의 장애물 코스가 생성되었습니다!");
    }

    private static void DestroyIfExists(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null) Object.DestroyImmediate(obj);
    }

    private static void CreateBlock(string name, Vector3 pos, Vector3 scale, Vector3 rot, Transform parent)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent);
        block.transform.position = pos;
        block.transform.localScale = scale;
        block.transform.rotation = Quaternion.Euler(rot);
    }

    private static WheelCollider CreateWheelCollider(string name, Vector3 localPos, Transform parent)
    {
        GameObject wheelObj = new GameObject(name);
        wheelObj.transform.SetParent(parent);
        wheelObj.transform.localPosition = localPos;
        return wheelObj.AddComponent<WheelCollider>();
    }

    private static Transform CreateWheelModel(string name, Vector3 localPos, Transform parent)
    {
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        model.name = name;
        model.transform.SetParent(parent);
        model.transform.localPosition = localPos;
        
        // 바퀴 모양으로 납작하게 만들고 눕히기
        model.transform.localScale = new Vector3(1f, 0.2f, 1f);
        model.transform.localRotation = Quaternion.Euler(0, 0, 90f);

        // 시각 전용이므로 콜라이더 제거
        Collider col = model.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        return model.transform;
    }
}
