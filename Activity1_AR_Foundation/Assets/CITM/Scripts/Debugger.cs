using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualCardDebugger : MonoBehaviour
{
    public GameManager gameManager;
    private CardAppearEffect cardAppearEffect;

    [Header("Models 3D")]
    public GameObject[] pokemonPrefabs;

    [Header("Materials")]
    public Material cardBackMaterial; // Material parte de atras
    public Material[] cardFrontMaterials; // Lista de materiales de la parte de delante de las cartas

    // Lista de memoria para guardar todas las cartas creadas y poderlas destruir si se reinicia el juego
    private List<GameObject> spawnedCards = new List<GameObject>();

    public void SpawnVirtualCards()
    {
        ClearVirtualCards();
        gameManager.StartGameWithCards(36);// Avisar al GameManager que habran 8 cartas y 4 parejas

        if (cardAppearEffect == null)
            cardAppearEffect = FindAnyObjectByType<CardAppearEffect>();

        List<string> cardNames = new List<string>();
        for (int i = 1; i <= 18; i++)
        {
            cardNames.Add("Image" + i);
            cardNames.Add("Image" + i);
        }

        // Mezcla las cartas para que no siempre sea igual el patron
        for (int i = 0; i < cardNames.Count; i++)
        {
            string temp = cardNames[i];
            int r = Random.Range(i, cardNames.Count);
            cardNames[i] = cardNames[r];
            cardNames[r] = temp;
        }

        // Poner las cartas en un grid de 6 x 6 , 36 cartas
        int index = 0;
        for (int x = 0; x < 6; x++)
        {
            for (int z = 0; z < 6; z++)
            {
                string pkmName = cardNames[index];
                // Poner bien el indice porque en Unity empieza en 0
                int pkmIndex = int.Parse(pkmName.Replace("Image", "")) - 1;

                // Crea el container del cubo, la 'carta'
                GameObject cardContainer = new GameObject("VirtualCard_" + index);
                cardContainer.transform.position = new Vector3(x * 1.5f - 3.75f, -1f, z * 1.5f + 2f);

                // Children del contenedor inicial, es la parte visual de la 'carta', el objeto que va a rotar 180 al hacerle click
                GameObject cardVisual = new GameObject("CardVisual");
                cardVisual.transform.SetParent(cardContainer.transform);
                cardVisual.transform.localPosition = Vector3.zero;

                // Grosor de la carta
                GameObject cardBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cardBody.transform.SetParent(cardVisual.transform);
                cardBody.transform.localPosition = Vector3.zero;
                cardBody.transform.localScale = new Vector3(1f, 0.02f, 1f); // Tamano de la carta
                Destroy(cardBody.GetComponent<BoxCollider>()); // Quitar colision que pone por defecto

                // Reverso
                GameObject backFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backFace.transform.SetParent(cardVisual.transform);
                backFace.transform.localPosition = new Vector3(0, 0.011f, 0); // Subida un poquito para que no clipee
                backFace.transform.localRotation = Quaternion.Euler(90, 0, 0); // Y rotarlo para que mire hacia arriba
                Destroy(backFace.GetComponent<MeshCollider>());
                if (cardBackMaterial != null) backFace.GetComponent<MeshRenderer>().material = cardBackMaterial;

                // Cara de la carta, png pokemon
                GameObject frontFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
                frontFace.transform.SetParent(cardVisual.transform);
                frontFace.transform.localPosition = new Vector3(0, -0.011f, 0); // Un poco para abajo por lo mismo
                frontFace.transform.localRotation = Quaternion.Euler(-90, 180, 0); // Y lo generamos mirando hacia el suelo
                Destroy(frontFace.GetComponent<MeshCollider>());
                if (cardFrontMaterials != null && pkmIndex < cardFrontMaterials.Length)
                    frontFace.GetComponent<MeshRenderer>().material = cardFrontMaterials[pkmIndex];

                // Box collider para poder hacer click
                BoxCollider collider = cardVisual.AddComponent<BoxCollider>();
                collider.size = new Vector3(1f, 0.1f, 1.4f);

                VirtualCardClick clicker = cardVisual.AddComponent<VirtualCardClick>();
                clicker.cardId = cardContainer.name;
                clicker.cardName = pkmName;
                clicker.manager = gameManager;
                clicker.appearEffect = cardAppearEffect;

                // Pokemon
                if (pokemonPrefabs != null && pkmIndex < pokemonPrefabs.Length)
                {
                    GameObject pkm = Instantiate(pokemonPrefabs[pkmIndex], cardContainer.transform);
                    pkm.transform.localPosition = new Vector3(0, 0.05f, 0);
                    pkm.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    pkm.SetActive(false);
                    clicker.pokemonModel = pkm;
                }

                spawnedCards.Add(cardContainer);
                index++;
            }
        }
    }

    public void ClearVirtualCards()
    {
        // Para eliminar todos los objetos virtuales creados
        foreach (var c in spawnedCards)
            if (c != null) Destroy(c);
        spawnedCards.Clear();
    }

    void Update()
    {
        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        // Detectar el click en el ordenador
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Mouse.current.position.ReadValue();
        }
        // Detectar el toque de pantalla en movil
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        // Lanzar el Raycast
        if (inputDetected)
        {
            // Si el Raycast choca con un collider que tiene el VirtualCardClick entonces gira la carta virtual
            Ray ray = Camera.main.ScreenPointToRay(inputPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                VirtualCardClick clicker = hit.collider.GetComponent<VirtualCardClick>();
                if (clicker != null) clicker.ToggleCard();
            }
        }
    }
}

public class VirtualCardClick : MonoBehaviour
{
    public string cardId; // Id unico de la carta
    public string cardName; // Nombre asignado, image2
    public GameManager manager;
    public GameObject pokemonModel;
    public CardAppearEffect appearEffect; // Efecto de la aparicion

    private bool isFaceUp = false; // Empieza boca abajo
    private bool isAnimating = false;

    public void ToggleCard()
    {
        // Si ya se esta girando entonces ignorar el click nuevo
        if (isAnimating) return;

        isFaceUp = !isFaceUp;
        StartCoroutine(FlipAnimation(isFaceUp));
    }

    private IEnumerator FlipAnimation(bool turningFaceUp)
    {
        isAnimating = true;

        // Animacion de desaparecer si se va a girar
        if (!turningFaceUp && appearEffect != null && pokemonModel != null)
        {
            appearEffect.PlayVirtualEffect(pokemonModel.transform, false);
        }

        Quaternion startRotation = transform.localRotation;
        Quaternion endRotation = turningFaceUp ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;

        float duration = 0.4f; // 0,4 s en girar
        float elapsed = 0f;
        bool modelSwitched = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentage = elapsed / duration;

            // Girar suavemente
            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, percentage);

            // A la mitad del giro entonces mostrar o ocultar el pokemon y efectos
            if (percentage >= 0.5f && !modelSwitched)
            {
                if (turningFaceUp)
                {
                    if (appearEffect != null && pokemonModel != null)
                        appearEffect.PlayVirtualEffect(pokemonModel.transform, true);
                    else if (pokemonModel != null)
                        pokemonModel.SetActive(true);
                }
                else if (appearEffect == null && pokemonModel != null)
                {
                    pokemonModel.SetActive(false);
                }

                modelSwitched = true;
            }

            yield return null; // Esperar al siguiente frame
        }

        // Cuando ha acabado la rotacion
        transform.localRotation = endRotation;

        // Aviso al GameManager
        if (turningFaceUp) manager.ProcessCardFlippedUp(cardId, cardName);
        else manager.ProcessCardFlippedDown(cardId);

        isAnimating = false; // Se puede volver a hacer click
    }
}