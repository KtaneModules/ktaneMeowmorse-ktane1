using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class MeowmorseScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;

    public MeshRenderer LED;
    public MeshRenderer[] earsLED;
    public Material unlit, lit;
    public KMSelectable up, down, topRightButton, display, hex;
    public KMSelectable[] catEars;
    public SpriteRenderer catPosition;
    public Transform spritePosition;
    public MeshRenderer buttonMesh;
    public MeshRenderer[] allMeshes;
    public Sprite[] allCatPics;
    public Sprite hatocat;
    public AudioClip[] shortMeows;
    public AudioClip[] longMeows;

    private Coroutine flash;
    private float holdingTime = 0f;
    private bool buttonHeld;
    private float initTime;

    private int buttonPresses, validPresses;
    public float timeBetweenPresses;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool isPlaying, moduleSolved;

    private int targetMeow;
    private int currentIndex = 0;

    private string[] catNames = new string[] { "Luna", "Oliver", "Bella", "Leo", "Max", "Tama", "Sora", "Kitty", "Sugar", "Siger", "Tiger", "Alice", "Melody", "Tom", "Nyan" };
    private string chosenName;
    private List<string> encodedSong = new List<string>();
    private string currentInput, finalSolution;
    private int meowPitch;
    private bool maxRule, tomRule;

    void Awake () {
        moduleId = moduleIdCounter++;
        
        up.OnInteract += delegate () { Up(); return false; };
        down.OnInteract += delegate () { Down(); return false; };
        topRightButton.OnInteract += delegate () { ButtonHandler(); return false; };
        topRightButton.OnInteractEnded += delegate () { ButtonHandlerEnded(); };
        display.OnInteract += delegate () { DisplayHandler(); return false; };
        hex.OnInteract += () => { ButtonHolder(); return false; };
        hex.OnInteractEnded += () => { HexHandler(); };
        for (int i = 0; i < catEars.Length; i++)
        {
            int k = i;
            catEars[k].OnInteract += () => { EarHandler(k); return false; };
            catEars[k].OnInteractEnded += () => { EarHandlerEnded(k); };
        }
    }

    void Start()
    {
        initTime = Bomb.GetTime();

        currentIndex = UnityEngine.Random.Range(0, 3);
        catPosition.sprite = allCatPics[currentIndex];

        //Determining targetMeow
        if (Bomb.GetIndicators().Count() < 3)
        {
            var letters = Bomb.GetSerialNumber();
            foreach (char a in letters)
            {
                if ("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(a))
                    targetMeow += a - 64;
                if ("ME0W".Contains(a))
                    targetMeow -= 1;
            }
            targetMeow %= 3;
        }
        else
        {
            var ind = Bomb.GetIndicators().ToArray();
            string targetString = "";
            StringBuilder sb = new StringBuilder();

            Array.Sort(ind);
            for(int i = 0; i < 3; i++)
            {
                if (Bomb.IsIndicatorOn(ind[i])) 
                    sb.Append(ind[i][0]);
                else 
                    sb.Append(ind[i][2]);
            }
            if (sb.ToString() == "CAT")
                targetMeow = 1;
            else
            {
                targetString = sb.ToString();
                foreach (char a in targetString)
                    targetMeow += a - 64;
                targetMeow -= Bomb.GetSerialNumber().Count(ch => "AEIOU".Contains(ch));

                targetMeow %= 3;
            }
        }
        var toneNames = new string[] { "low", "middle", "high" };
        Debug.LogFormat("[Meowmorse #{0}] Target meow is {1} tone.", moduleId, toneNames[targetMeow]);

        GenerateSong();
        GenerateSolution(true);
    }

    void GenerateSong()
    {
        //Triple the chances of each cat other than Nyan appearing
        chosenName = catNames[UnityEngine.Random.Range(0, catNames.Length * 3 - 2)/ 3];
        encodedSong = new List<string>();
        string encodedName = Data.GenerateSequence(chosenName.ToUpper());
        var toneNames = new string[] { "Low", "Middle", "High" };
        StringBuilder sb = new StringBuilder();
        foreach (char i in encodedName)
        {
            int lengthRandomizer = UnityEngine.Random.Range(0, 2);//0 for short, 1 for long
            int pitchRandomizer = UnityEngine.Random.Range(0, 3);//0 for low, 1 for middle, 2 for high
            switch (i)
            {
                case '.':
                    if (lengthRandomizer == 1)
                    {
                        sb.Append("Long " + toneNames[targetMeow]);
                    }
                    else
                    {
                        sb.Append("Short ");
                        while (pitchRandomizer == targetMeow)
                            pitchRandomizer = UnityEngine.Random.Range(0, 3);
                        sb.Append(toneNames[pitchRandomizer]);
                    }
                    break;
                case '-':
                    if (lengthRandomizer == 0)
                    {
                        sb.Append("Short " + toneNames[targetMeow]);
                    }
                    else
                    {
                        sb.Append("Long ");
                        while (pitchRandomizer == targetMeow)
                            pitchRandomizer = UnityEngine.Random.Range(0, 3);
                        sb.Append(toneNames[pitchRandomizer]);
                    }
                    break;
                default:
                    sb.Append("");
                    break;
            }
            encodedSong.Add(sb.ToString());
            sb.Remove(0, sb.Length);
        }

        foreach (string song in encodedSong)
        {
            if (song == "")
                sb.Append("blank, ");
            else
                sb.Append(song + " tone, ");
        }
        sb.Remove(sb.Length - 2, 2);
        Debug.LogFormat("[Meowmorse #{0}] The whole song is {1}.", moduleId, sb.ToString().ToLower());
        Debug.LogFormat("[Meowmorse #{0}] The chosen cat name is {1}.", moduleId, chosenName);
    }

    void GenerateSolution(bool log)
    {
        string initSol = Data.TargetWords[chosenName];
        finalSolution = "";

        switch (chosenName)
        {
            case "Luna":
                meowPitch = 2;
                break;
            case "Oliver":
                meowPitch = 0;
                break;
            case "Bella":
                meowPitch = 1;
                break;
            case "Leo":
                if (Bomb.GetModuleNames().Contains("Astrology") || Bomb.GetModuleNames().Contains("Constellations"))
                    meowPitch = 2;
                else
                    meowPitch = 0;
                break;
            case "Max":
                meowPitch = 0;
                maxRule = Bomb.GetSerialNumberNumbers().Max() > 7;
                break;
            case "Tama":
                if (Bomb.IsIndicatorOn(Indicator.BOB))
                    meowPitch = 1;
                else
                    meowPitch = 2;
                if (Bomb.GetBatteryCount() > 2)
                    initSol = "pochi";
                break;
            case "Sora":
                if (targetMeow == 1)
                    meowPitch = 2;
                else
                    meowPitch = 1;
                break;
            case "Kitty":
                if (Bomb.GetBatteryCount() % 2 == 0)
                    meowPitch = 0;
                else
                    meowPitch = 2;
                break;
            case "Sugar":
                if (Bomb.IsPortPresent(Port.Parallel))
                    meowPitch = 2;
                else
                    meowPitch = 0;
                break;
            case "Siger":
                if (string.Join("", Bomb.GetIndicators().ToArray()).Contains('T') || string.Join("", Bomb.GetIndicators().ToArray()).Contains('M'))
                    meowPitch = 2;
                else
                    meowPitch = 1;
                break;
            case "Tiger":
                meowPitch = 1 + (int)(initTime / 60) + 1;
                meowPitch %= 3;
                break;
            case "Alice":
                meowPitch = 2 + Bomb.GetSolvedModuleNames().Count() + 1;
                meowPitch %= 3;
                break;
            case "Melody":
                meowPitch = 1;
                break;
            case "Tom":
                meowPitch = 0;
                tomRule = Bomb.GetModuleNames().Contains("Mouse In The Maze");
                break;
            case "Nyan":
                meowPitch = 2 + Bomb.GetSolvedModuleNames().Count() + 1;
                meowPitch %= 3;
                string temp = initSol;
                if (Bomb.IsIndicatorOn(Indicator.CAR) && Bomb.IsIndicatorOn(Indicator.TRN))
                    temp = "nyan";
                for (int i = 0; i < (Bomb.GetSolvableModuleNames().Count() - Bomb.GetSolvedModuleNames().Count()) % 10 + 1; i++)
                    initSol += temp;
                break;
            default:
                break;
        }
        finalSolution = initSol;
        if (log)
        {
            if (tomRule)
                Debug.LogFormat("[Meowmorse #{0}] Tom's unicorn rule is active, thus press the submit button three times quickly.", moduleId);
            else
                Debug.LogFormat("[Meowmorse #{0}] The string to submit is {1}.", moduleId, finalSolution);

            if (maxRule)
                Debug.LogFormat("[Meowmorse #{0}] Max's rule is active, thus the correct ear to press is the {1} ear.", moduleId, Bomb.GetSerialNumber()[5] % 2 != 0 ? "right" : "left");
            else
                Debug.LogFormat("[Meowmorse #{0}] The correct ear to press is the {1} ear.", moduleId, Bomb.GetSerialNumber()[5] % 2 == 0 ? "right" : "left");
        }
    }

    void Up()
    {
        if (moduleSolved)
            return;
        up.AddInteractionPunch(0.2f);
        Audio.PlaySoundAtTransform("meow2", up.transform);
        currentIndex++;
        currentIndex %= 3;
        catPosition.sprite = allCatPics[currentIndex];
    }
    void Down()
    {
        if (moduleSolved)
            return;
        down.AddInteractionPunch(0.2f);
        Audio.PlaySoundAtTransform("meow6", down.transform);
        currentIndex += 2; //Same as decrementing.
        currentIndex %= 3;
        catPosition.sprite = allCatPics[currentIndex];
    }

    void DisplayHandler()
    {
        display.AddInteractionPunch(0.5f);
        if (moduleSolved)
            return;
        Audio.PlaySoundAtTransform(shortMeows[1].name, transform);
    }

    void ButtonHandler()
    {
        if (moduleSolved)
            return;
        display.AddInteractionPunch(0.5f);
        if (isPlaying)
            return;
        buttonHeld = true;
        StartCoroutine(HoldToClear());
    }

    void ButtonHandlerEnded()
    {
        if (moduleSolved || isPlaying)
            return;
        buttonHeld = false;
        
        if (holdingTime > 1f)
        {
            currentInput = null;
            buttonMesh.material.color = "00756B".Color();
            Audio.PlaySoundAtTransform("meow5", down.transform);
            Debug.LogFormat("<Meowmorse #{0}> Sufficient time held, clearing input...", moduleId);
        }
        else if (currentInput == null && !tomRule)
        {
            StartCoroutine("Playing");
            Debug.LogFormat("<Meowmorse #{0}> Playing the song...", moduleId);
        }
        else if (tomRule)
        {
            if (buttonPresses == 0)
                StartCoroutine(MashChecker());
            else
            {
                if (timeBetweenPresses < 0.5f)
                    validPresses++;
                else
                    validPresses = 0;
                timeBetweenPresses = 0f;
            }
            buttonPresses++;

            if (validPresses == 3)
            {
                Debug.LogFormat("[Meowmorse #{0}] Submit button pressed correctly, module solved meow.", moduleId);
                moduleSolved = true;
                Audio.PlaySoundAtTransform("tom", transform);
                StartCoroutine(Solve());
            }
        }
        else
        {
            GenerateSolution(false);//Update for solved module related rules
            var toneNames = new string[] { "low", "middle", "high" };
            Debug.LogFormat("[Meowmorse #{0}] Submit button pressed at {1} solved module(s)! Checking input...", moduleId, Bomb.GetSolvedModuleNames().Count());
            Debug.LogFormat("[Meowmorse #{0}] The meow pitch is {1} tone.", moduleId, toneNames[meowPitch]);
            if (chosenName == "Melody")
                Debug.LogFormat("[Meowmorse #{0}] Melody is the chosen cat, thus the meow pitch would cycle by one for every letter sent.", moduleId);

            string expectedCode = Data.GenerateSequence(finalSolution.ToUpper()).Trim();
            Debug.LogFormat("[Meowmorse #{0}] Expected solution in morse code: {1}", moduleId, expectedCode);

            //Debug.Log(currentInput);
            string[] parameters = currentInput.Trim().Split(' ');
            bool pitchDisplayedCorrect = true;
            StringBuilder sb = new StringBuilder();
            foreach (string code in parameters)
            {
                switch (code[0])
                {
                    case '-':
                        if (code[1] - '0' != targetMeow)
                            sb.Append("-");
                        else
                            sb.Append(".");
                        break;
                    case '.':
                        if (code[1] - '0' != targetMeow)
                            sb.Append(".");
                        else
                            sb.Append("-");

                        break;
                    case 'X':
                        sb.Append(" ");
                        if (chosenName == "Melody")
                        {
                            meowPitch += 1;
                            meowPitch %= 3;
                        }
                        break;
                    default:
                        break;
                }
                if (code.Length > 1)
                {
                    if (code[1] - '0' != meowPitch)
                        pitchDisplayedCorrect = false;
                }
                    
                

                
            }
            Debug.LogFormat("[Meowmorse #{0}] The morse inputs are as follows: {1}", moduleId, sb.ToString());


            if (sb.ToString() == expectedCode && pitchDisplayedCorrect)
            {
                Debug.LogFormat("[Meowmorse #{0}] Phrase inputted correctly, with the correct meow pitch displayed. Module solved meow.", moduleId);
                moduleSolved = true;
                if (chosenName == "Nyan")
                    StartCoroutine(NyanSolve());
                else
                    StartCoroutine(Solve());


            }
            else
            {
                if (sb.ToString() != expectedCode)
                    Debug.LogFormat("[Meowmorse #{0}] Phrase is inputted incorrectly.", moduleId);
                if (!pitchDisplayedCorrect)
                    Debug.LogFormat("[Meowmorse #{0}] Meow pitch is incorrectly displayed for a letter at some point.", moduleId);
                Debug.LogFormat("[Meowmorse #{0}] Strike and reset meow.", moduleId);
                Module.HandleStrike();
                Audio.PlaySoundAtTransform("huh", transform);
                currentInput = null;
                GenerateSong();
                GenerateSolution(true);
            }
        }
        holdingTime = 0f;
    }

    void EarHandler(int k)
    {
        if (moduleSolved || isPlaying)
            return;
        catEars[k].AddInteractionPunch(0.5f);
        earsLED[k].material = lit;
        if (Bomb.GetSerialNumber()[5] % 2 != k ^ maxRule)
        {
            currentInput += 'X';//Denoting space
            currentInput += ' ';
            Debug.LogFormat("<Meowmorse #{0}> Correct ear selected, space inputted", moduleId, holdingTime);
            Audio.PlaySoundAtTransform("meow1", transform);
        }
        else
        {
            Debug.LogFormat("[Meowmorse #{0}] Wrong ear selected, strike meow.", moduleId);
            Module.HandleStrike();
            Audio.PlaySoundAtTransform("huh", transform);
        }
    }

    void EarHandlerEnded(int k)
    {
        if (moduleSolved)
            return;
        earsLED[k].material = unlit;
    }

    void ButtonHolder()
    {
        if (moduleSolved || isPlaying)
            return;
        hex.AddInteractionPunch(0.5f);
        LED.material = lit;
        Audio.PlaySoundAtTransform("purr", transform);
        buttonHeld = true;
        StartCoroutine(ButtonHolding());
    }

    void HexHandler()
    {
        if (moduleSolved)
            return;
        buttonHeld = false;
        LED.material = unlit;
        if (isPlaying)
            return;
        if (tomRule)
        {
            Module.HandleStrike();
            Debug.LogFormat("[Meowmorse #{0}] Pressed the hex while Tom unicorn rule is active, strike meow.", moduleId);
            Audio.PlaySoundAtTransform("huh", transform);
            holdingTime = 0f;
            return;
        }
        else if (holdingTime > 1f)
        {
            currentInput += '-';
            Debug.LogFormat("<Meowmorse #{0}> Hold time is {1} seconds, long press inputted", moduleId, holdingTime);

        }
        else
        {
            currentInput += '.';
            Debug.LogFormat("<Meowmorse #{0}> Hold time is {1} seconds, short press inputted", moduleId, holdingTime);
        }

        currentInput += currentIndex;
        currentInput += ' ';
        holdingTime = 0f;
    }

    IEnumerator ButtonHolding()
    {
        while (buttonHeld)
        {
            yield return null;
            holdingTime += Time.deltaTime;
        }
    }

    IEnumerator HoldToClear()
    {
        while (buttonHeld)
        {
            yield return null;
            holdingTime += Time.deltaTime;
            if (holdingTime > 1f)
            {
                buttonMesh.material.color = "FFFFFF".Color();
            }
        }
    }

    IEnumerator MashChecker()
    {
        while (!moduleSolved)
        {
            yield return null;
            timeBetweenPresses += Time.deltaTime;
        }
    }

    IEnumerator Playing()
    {
        isPlaying = true;
        buttonMesh.material.color = "FF0000".Color();
        foreach (string sound in encodedSong)
        {
            int index = 0;
            if (sound == "")
            {
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                Audio.PlaySoundAtTransform(sound + " meow", transform);

                if (sound.Contains("Low"))
                    index = 0;
                if (sound.Contains("Middle"))
                    index = 1;
                if (sound.Contains("High"))
                    index = 2;

                if (sound.Contains("Short"))
                    yield return new WaitForSeconds(shortMeows[index].length);
                if (sound.Contains("Long"))
                    yield return new WaitForSeconds(longMeows[index].length);

                yield return null;

            }
        }
        buttonMesh.material.color = "00756B".Color();
        isPlaying = false;
    }

    IEnumerator Solve()
    {
        Audio.PlaySoundAtTransform("solved", transform);
        foreach (MeshRenderer renderer in allMeshes)
        {
            renderer.material.color = "00FF64".Color();
            yield return new WaitForSeconds(0.1f);
        }
        Module.HandlePass();

    }

    IEnumerator NyanSolve()
    {
        foreach (MeshRenderer rend in allMeshes)
        {
            rend.material.color = "000000".Color();
        }
        catPosition.sprite = null;
        yield return null;
        Audio.PlaySoundAtTransform("nyan", transform);
        float delta = 0;
        while (delta < 3.858f)
        {
            delta += Time.deltaTime;
            yield return null;
        }
        Module.HandlePass();
        catPosition.sprite = hatocat;
        spritePosition.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        for (int i = 0; i < allMeshes.Length; i++)
        {
            StartCoroutine(Rainbow(i));
        }
        StartCoroutine(Rotator());
    }

    IEnumerator Rainbow(int k)
    {
        float hue = 0f;
        while (moduleSolved)
        {
            allMeshes[k].material.color = Color.HSVToRGB(hue, 0.5f, 1);
            hue += 0.3f * Time.deltaTime;
            hue %= 1;
            yield return null;
        }
    }

    IEnumerator Rotator()
    {
        float newAngle = 0f;
        while (moduleSolved)
        {
            spritePosition.localEulerAngles = new Vector3(0f, 0f, newAngle);
            newAngle += 60f * Time.deltaTime;
            newAngle %= 360f;
            yield return null;
        }
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"<!{0} press button> to press the top right button, <!{0} press up> or <!{0} press down> to cycle the display up or down respectively, <!{0} set low/middle/high> to set the display on a specific tone, <!{0} press left> or <!{0} press right> to press the left or right ear buttons respectively, <!{0} send .--> to send a message with short and long presses using the hex button (you could only send one letter at a time), <!{0} clear> to hold the top right button for clearing input, <!{0} mash> to mash the top right button";
    #pragma warning restore 414
    
    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.ToLowerInvariant().Trim();
        string[] parameters = command.Split(' ');
        if (parameters[0] == "press")
        {
            yield return null;
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify what button to press!";
                yield break;
            }
            else if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
                yield break;
            }
            else
            {
                switch (parameters[1])
                {
                    case "button":
                        topRightButton.OnInteract();
                        yield return null;
                        topRightButton.OnInteractEnded();
                        yield return null;
                        break;

                    case "up":
                        up.OnInteract();
                        yield return null;
                        break;

                    case "down":
                        down.OnInteract();
                        yield return null;
                        break;

                    case "left":
                        catEars[0].OnInteract();
                        yield return null;
                        catEars[0].OnInteractEnded();
                        yield return null;
                        break;

                    case "right":
                        catEars[1].OnInteract();
                        yield return null;
                        catEars[1].OnInteractEnded();
                        yield return null;
                        break;

                    default:
                        yield return "sendtochaterror Invalid button to press.";
                        yield break;
                }
            }
        }
        else if (parameters[0] == "set")
        {
            yield return null;
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify which display to set!";
                yield break;
            }
            else if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
                yield break;
            }
            else
            {
                int set = 0;
                switch (parameters[1])
                {
                    case "low":
                        set = 0;
                        break;

                    case "middle":
                        set = 1;
                        break;

                    case "high":
                        set = 2;
                        break;

                    default:
                        yield return "sendtochaterror Invalid display to set.";
                        yield break;
                }
                while (currentIndex != set)
                {
                    up.OnInteract();
                    yield return null;
                }
            }
        }
        else if (parameters[0] == "send")
        {
            yield return null;
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify what to send!";
                yield break;
            }
            else if (parameters.Length > 2)
            {
                yield return "sendtochaterror No spaces in the message, please!";
                yield break;
            }

            Match m = Regex.Match(parameters[1], @"^[.-]+$");

            if (m.Success)
            {
                foreach (char a in parameters[1])
                {
                    switch (a)
                    {
                        case '.':
                            hex.OnInteract();
                            yield return new WaitForSeconds(0.2f);
                            hex.OnInteractEnded();
                            yield return null;
                            break;

                        case '-':
                            hex.OnInteract();
                            while (holdingTime <= 1f)
                                yield return null;
                            hex.OnInteractEnded();
                            yield return null;
                            break;

                        default:
                            yield return "sendtochaterror This shouldn't happen, shit";
                            yield break;
                    }
                }
            }
            else
            {
                yield return "sendtochaterror Invalid message to send.";
                yield break;
            }
        }
        else if (command == "clear")
        {
            topRightButton.OnInteract();
            while (holdingTime < 1f)
                yield return null;
            topRightButton.OnInteractEnded();
            yield return null;
        }
        else if (command == "mash")
        {
            for (int i = 0; i < 3; i++)
            {
                topRightButton.OnInteract();
                yield return null;
                topRightButton.OnInteractEnded();
                yield return null;
            }
        }
        else if (command == "meow")
        {
            yield return "sendtochat meow~";
            yield break;
        }
        else if (command == "nya")
        {
            yield return "sendtochat nya~";
            yield break;
        }
        else
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
    }


    IEnumerator TwitchHandleForcedSolve ()
    {
        while (!moduleSolved)
        {
            if (isPlaying)
                yield return null;
            else
            {
                if (currentInput != null)
                {
                    topRightButton.OnInteract();
                    while (holdingTime < 1f)
                        yield return null;
                    topRightButton.OnInteractEnded();
                    yield return null;
                }

                if (tomRule)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        topRightButton.OnInteract();
                        yield return null;
                        topRightButton.OnInteractEnded();
                        yield return null;
                    }
                }
                else
                {
                    string codeToEnter = Data.GenerateSequence(finalSolution.ToUpper()).Trim();
                    int pitch = meowPitch;
                    foreach (char c in codeToEnter)
                    {
                        while (currentIndex != pitch)
                        {
                            up.OnInteract();
                            yield return null;
                        }

                        switch (c)
                        {
                            case '.':
                                if (pitch != targetMeow)
                                {
                                    hex.OnInteract();
                                    yield return new WaitForSeconds(0.2f);
                                    hex.OnInteractEnded();
                                    yield return null;
                                }
                                else
                                {
                                    hex.OnInteract();
                                    while (holdingTime <= 1f)
                                        yield return null;
                                    hex.OnInteractEnded();
                                    yield return null;
                                }
                                break;
                            case '-':
                                if (pitch != targetMeow)
                                {
                                    hex.OnInteract();
                                    while (holdingTime <= 1f)
                                        yield return null;
                                    hex.OnInteractEnded();
                                    yield return null;
                                }
                                else
                                {
                                    hex.OnInteract();
                                    yield return new WaitForSeconds(0.2f);
                                    hex.OnInteractEnded();
                                    yield return null;
                                }
                                break;
                            case ' ':
                                if (maxRule)
                                {
                                    catEars[Bomb.GetSerialNumber()[5] % 2].OnInteract();
                                    yield return null;
                                    catEars[Bomb.GetSerialNumber()[5] % 2].OnInteractEnded();
                                }
                                else
                                {
                                    catEars[1 - Bomb.GetSerialNumber()[5] % 2].OnInteract();
                                    yield return null;
                                    catEars[1 - Bomb.GetSerialNumber()[5] % 2].OnInteractEnded();
                                }

                                if (chosenName == "Melody")
                                {
                                    pitch++;
                                    pitch %= 3;
                                }
                                break;
                            default:
                                break;
                        }
                        yield return null;

                    }
                    topRightButton.OnInteract();
                    yield return null;
                    topRightButton.OnInteractEnded();
                    yield return null;
                }
            }
        }
    }
}
