using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;
using Words;

using Rnd = UnityEngine.Random;

public class furCipherScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public TextMesh[] screenTexts;
    public TextMesh submitText;
    public KMSelectable leftArrow;
    public KMSelectable rightArrow;
    public KMSelectable submit;
    public KMSelectable[] keyboard;
    public TextMesh[] arrowTexts;

    private string getKey(string kw, string alphabet, bool kwFirst)
    {
        return (kwFirst ? (kw + alphabet) : alphabet.Except(kw).Concat(kw)).Distinct().Join("");
    }

    
    
    #region UI, TP
    //protected PageInfo[] pages;
    protected String[][] pages = new String[][]{
        new String[3],
        new String[3]
    };
    protected KMBombModule module;
    protected int page;
    protected bool submitScreen;
    protected string answer;
    protected bool moduleSolved;
    protected bool moduleSelected;
    static int moduleIdCounter = 1;
	int moduleId;
    Data data = new Data();
    string baseAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    protected void Initialize(Boolean inverted = false){
        answer = data.PickWord(6);
        Log("Generated Word: {0}", answer);
        if (inverted)
            InvertedPart();
        else{

        //Step 1 OWO Cipher
        Log("OWO Cipher:");
        string kw1 = data.PickWord(6);
        Log("Chosen KW1: {0}", kw1);

        string[] reordered = generateAndReorderOWO();

        string key2 = "";
        for (int i = 0; i < 6; i++){
            char keyLetter = (char)(((kw1[i] - reordered[i][0] - reordered[i][1] - reordered[i][2] + 128 +26*3)%26)+64);
            keyLetter = keyLetter == '@' ? 'Z' : keyLetter;
            key2 += keyLetter;
            Log("{0} --{1}-> {2}", keyLetter, reordered[i], kw1[i]);
        }
        Log("generated key 2: {0}", key2);
        pages[0][2] = key2;

        
            //Step 3 Monosodium Glutamate Cipher
            Log("Monosodium Glutamate Cipher:");
            string kw2 = data.PickWord(3,8);
            string kw3 = data.PickWord(3,8);
            Log("KW2: {0}", kw2);
            pages[1][1] = kw2;
            Log("KW3: {0}", kw3);
            pages[1][2] = kw3;

            string A2 = getKey(kw2, baseAlphabet, (Bomb.GetSerialNumberNumbers().First() % 2 == 0));
            Log("A2: {0}", A2);
            string A3 = getKey(kw3, baseAlphabet, (Bomb.GetBatteryCount() % 2 == 0));
            Log("A3: {0}", A3);
            bool topLeft = kw1[0]%2 == 0; //true means top starts with letter; otherwise top starts with space
            int[] numbers;
            numbers = kw1[5] < 'N' ? new int[]{9,2,6,6,2,1} : new int[]{6,2,1,9,2,6};
            string encrypted = "";
            for(int i = 0; i < 6; i++){
                string logsteps = answer[i] + "";
                int move = kw1[i] - 64;
                int index = A3.IndexOf(answer[i]);
                index = index-move < 0 ? index-move+26 : index-move;
                logsteps += " -> " + A3[index];
                char outer = A2[convertInnerToOuter(topLeft, index)];
                logsteps += "/" + outer;
                index = A3.IndexOf(outer);
                logsteps += " -> " + A3[index];
                outer = A2[convertInnerToOuter(topLeft, index)];
                logsteps += "/" + outer;
                index = index + 2*numbers[i] > 25 ? index + 2*numbers[i]-26 : index + 2*numbers[i];
                outer = A2[convertInnerToOuter(topLeft, index)];
                logsteps += " -> " + outer;
                encrypted += outer;
                Log(logsteps);
            }
            Log("encrypted word: {0}", encrypted);
        
            //Step 2 Cheese Grater Cipher
            Log("Cheese Grater Cipher");
            topLeft = Bomb.GetSerialNumberNumbers().Last() % 2 == 0; //now topLeft signals row lengths 7676; false -> 6767
            string A1 = getKey(kw1, baseAlphabet, (Bomb.GetIndicators().Count() + Bomb.GetPortPlateCount()) > 4);
            Log("Alphabet: {0}", A1);
            int offset = Rnd.Range(3,18);
            foreach(char c in encrypted){
                offset += c -64;
            }
            Log("Offset: {0}", offset);
            string decrypted = encrypted;
            encrypted = "";
            string A12 = reorderAlphabetCheeseGrater(A1, topLeft);
            for (int i = 5; i >= 0; i--){
                offset = offset - decrypted[i] + 64;
                string logsteps = "Offset: " + offset + ",  " + decrypted[i];
                int right = offset % 7;
                int upLeft = ((offset - right)/7) %26;

                int index = A12.IndexOf(decrypted[i]);
                index = index-upLeft < 0 ? index - upLeft + 26 : index - upLeft;
                logsteps += " -> " + A12[index];
                index = A1.IndexOf(A12[index]);
                if (topLeft){
                    if (index < 7){
                        index = index + right > 6 ? index + right - 7 : index + right;
                    } else if (index < 13){
                        right %= 6;
                        index = index + right > 12 ? index + right - 6 : index + right;
                    } else if (index < 20){
                        index = index + right > 19 ? index + right - 7 : index + right;
                    }else{
                        right %= 6;
                        index = index + right > 25 ? index + right - 6 : index + right;
                    }
                }else{
                    if (index < 6){
                        right %= 6;
                        index = index + right > 5 ? index + right - 6 : index + right;
                    } else if (index < 13){
                        index = index + right > 12 ? index + right - 7 : index + right;
                    } else if (index < 19){
                        right %= 6;
                        index = index + right > 18 ? index + right - 6 : index + right;
                    }else{
                        index = index + right > 25 ? index + right - 7 : index + right;
                    }
                }
                logsteps += " -> " + A1[index];
                encrypted = A1[index] + encrypted;
                Log(logsteps);
            }
            pages[1][0] = offset.ToString();
            Log("Encrypted Word: {0}", encrypted);
            pages[0][0] = encrypted;

        } //end non inverted part
    }

    //TODO: write the inversion
    protected void InvertedPart(){
        //Step 1 OWO Cipher
        Log("OWO Cipher:");
        string kw1 = data.PickWord(6);
        Log("Chosen KW1: {0}", kw1);

        string[] reordered = generateAndReorderOWO();

        string key2 = "";
        for (int i = 0; i < 6; i++){
            char keyLetter = (char)(((kw1[i] + reordered[i][0] + reordered[i][1] + reordered[i][2] - 256)%26)+64);
            keyLetter = keyLetter == '@' ? 'Z' : keyLetter;
            key2 += keyLetter;
            Log("{0} --{1}-> {2}", keyLetter, reordered[i], kw1[i]);
        }
        Log("generated key 2: {0}", key2);
        pages[0][2] = key2;

        //Step 3 Cheese Grater Cipher
        Log("Cheese Grater Cipher");
            bool topLeft = Bomb.GetSerialNumberNumbers().Last() % 2 == 0; //topLeft signals row lengths 7676; false -> 6767
            string A1 = getKey(kw1, baseAlphabet, (Bomb.GetIndicators().Count() + Bomb.GetPortPlateCount()) > 4);
            Log("Alphabet: {0}", A1);
            int offset = Rnd.Range(3,18);
            Log("Offset: {0}", offset);
            string encrypted = "";
            string A12 = reorderAlphabetCheeseGrater(A1, topLeft);
            for (int i = 5; i >= 0; i--){
                int right = offset % 7;
                int upLeft = ((offset - right)/7) %26;
                int index = A1.IndexOf(answer[i]);

                if (topLeft){
                    if (index < 7){
                        index = index - right < 0 ? index - right + 7 : index - right;
                    } else if (index < 13){
                        right %= 6;
                        index = index - right < 7 ? index - right + 6 : index - right;
                    } else if (index < 20){
                        index = index - right < 13 ? index - right + 7 : index - right;
                    }else{
                        right %= 6;
                        index = index - right < 20 ? index - right + 6 : index - right;
                    }
                }else{
                    if (index < 6){
                        right %= 6;
                        index = index - right < 0 ? index - right + 6 : index - right;
                    } else if (index < 13){
                        index = index - right < 6 ? index - right + 7 : index - right;
                    } else if (index < 19){
                        right %= 6;
                        index = index - right < 13 ? index - right + 6 : index - right;
                    }else{
                        index = index - right < 20 ? index - right + 7 : index - right;
                    }
                }
                string logsteps = answer[i] + " -> " + A1[index];
                index = A12.IndexOf(A1[index]);
                index = (index+upLeft) % 26;
                logsteps += " -> " + A12[index];

                encrypted = A12[index] + encrypted;
                offset = offset + A12[index] - 64;
                logsteps += "; Offset: " + offset;
                Log(logsteps);
            }
            pages[1][0] = offset.ToString();
            Log("Encrypted Word: {0}", encrypted);
            pages[0][0] = encrypted;

        //Step 2 Monosodium Glutamate Cipher
            Log("Monosodium Glutamate Cipher:");
            string kw2 = data.PickWord(3,8);
            string kw3 = data.PickWord(3,8);
            Log("KW2: {0}", kw2);
            pages[1][1] = kw2;
            Log("KW3: {0}", kw3);
            pages[1][2] = kw3;

            string A2 = getKey(kw2, baseAlphabet, (Bomb.GetSerialNumberNumbers().First() % 2 == 0));
            Log("A2: {0}", A2);
            string A3 = getKey(kw3, baseAlphabet, (Bomb.GetBatteryCount() % 2 == 0));
            Log("A3: {0}", A3);
            topLeft = kw1[0]%2 == 0; //true means top starts with letter; otherwise top starts with space
            int[] numbers;
            numbers = kw1[5] < 'N' ? new int[]{9,2,6,6,2,1} : new int[]{6,2,1,9,2,6};
            string decrypted = encrypted;
            encrypted = "";

            for(int i = 0; i < 6; i++){
                string logsteps = decrypted[i] + "";
                int move = kw1[i] - 64;
                int index = A2.IndexOf(decrypted[i]);
                index = index > 12 ? (index - numbers[i] < 13 ? index-numbers[i] + 13 : index - numbers[i]) : (index - numbers[i] < 0 ? index - numbers[i] + 13 : index - numbers[i]);
                char inner = A3[convertOuterToInner(topLeft, index)];
                logsteps += String.Format(" -> {0}/{1}", A2[index], inner);
                index = A2.IndexOf(inner);
                logsteps += " -> " + A2[index];
                index = convertOuterToInner(topLeft, index);
                logsteps += "/" + A3[index];
                index = (index+move) % 26;
                inner = A3[index];
                logsteps += " -> " + inner;
                Log(logsteps);
                encrypted += inner;
            }
            Log("encrypted word: {0}", encrypted);
    }

    protected string[] generateAndReorderOWO(){
        string serialNumber = Bomb.GetSerialNumber();
        string validLetters = "CENOQU";
        string[] keyGrid = new string[6];
        string key1 = "";
        for (int i = 0; i < 6; i++){
            char addedLetter = validLetters[Rnd.Range(0, validLetters.Length)];
            key1 += addedLetter;
            
            //A (65-1) -> 1 (2-1); need to subctract 1 from letter characters
            //0 (48) -> 0; no adjustment for numbers
            int prev = serialNumber[i] < 'A' ? serialNumber[i] % 3 : (serialNumber[i] -1) % 3;
            for (int j = 0; j < 3; j++){
                if (j != prev)
                    keyGrid[i] += addedLetter;
                else
                    keyGrid[i] += 'W';
            }
        }
        pages[0][1] = key1;
        Log("Key 1: {0}", key1);
        string generatedOWO = "";
        string[] reordered = new string[6];
        for (int i = 0; i < 6; i++){
            reordered[i] = keyGrid[0 + (i%2)*3][(i - (i%2))/2] + "" + keyGrid[1 + (i%2)*3][(i - (i%2))/2] + "" + keyGrid[2 + (i%2)*3][(i - (i%2))/2];
            generatedOWO += reordered[i];
        }
        Log("reordered: {0}", generatedOWO);
        return reordered;
    }

    protected int convertOuterToInner(bool topLeft, int index){
        return topLeft ? (index < 13 ? index*2 : index*2 -25) : (index < 13 ? index*2 + 1 : index*2 - 26);
    }

    protected int convertInnerToOuter(bool topLeft, int index){
        return topLeft ? (index % 2 == 0 ? index/2 : 13 + (index-1)/2) : (index % 2 == 0 ? 13 + index/2 : (index-1)/2);
    }

    protected string reorderAlphabetCheeseGrater(string baseAlphabet, bool topLeft){
        string[] cheeseGrater = new string[4];
        if (topLeft){
            cheeseGrater[0] = baseAlphabet.Substring(0, 7);
            cheeseGrater[1] = baseAlphabet.Substring(7, 6);
            cheeseGrater[2] = baseAlphabet.Substring(13,7);
            cheeseGrater[3] = baseAlphabet.Substring(20,6);
        } else{
            cheeseGrater[0] = baseAlphabet.Substring(0,6);
            cheeseGrater[1] = baseAlphabet.Substring(6, 7);
            cheeseGrater[2] = baseAlphabet.Substring(13,6);
            cheeseGrater[3] = baseAlphabet.Substring(19,7);
        }

        int x = 0;
        int y = 0;
        string res = "";
        for (int i = 0; i<26; i++){
            res += cheeseGrater[y][x];
            if (cheeseGrater[y].Length == 6){
                x++;
                y = (y+1) % 4;
            } else{
                y = x == 6 ? (y+2) % 4 : (y+1) % 4;
                x = x%6;
            }
        }
        return res;
    }

    void Awake(){
		moduleId = moduleIdCounter++;
        module = GetComponent<KMBombModule>();
        leftArrow.OnInteract += delegate () { left(leftArrow); return false; };
        rightArrow.OnInteract += delegate () { right(rightArrow); return false; };
        submit.OnInteract += delegate () { submitWord(submit); return false; };
        foreach (KMSelectable keybutton in keyboard)
        {
            KMSelectable pressedButton = keybutton;
            pressedButton.OnInteract += delegate () { letterPress(pressedButton); return false; };
        }
	}

    void Start()
    {
        module.GetComponent<KMSelectable>().OnFocus += delegate { moduleSelected = true; };
        module.GetComponent<KMSelectable>().OnDefocus += delegate { moduleSelected = false; };
        Initialize();
        page = 0;
        getScreens();
    }

    protected virtual void left(KMSelectable arrow)
    {
        if (!moduleSolved)
        {
            Audio.PlaySoundAtTransform("ArrowPress", transform);
            submitScreen = false;
            arrow.AddInteractionPunch();
            page = (page - 1 + pages.Length) % pages.Length;
            getScreens();
        }
    }

    protected virtual void right(KMSelectable arrow)
    {
        if (!moduleSolved)
        {
            Audio.PlaySoundAtTransform("ArrowPress", transform);
            submitScreen = false;
            arrow.AddInteractionPunch();
            page = (page + 1) % pages.Length;
            getScreens();
        }
    }

    protected virtual void getScreens()
    {
        submitText.text = (page + 1).ToString();
        for (var screen = 0; screen < 3; screen++)
        {
            screenTexts[screen].text = screen < pages[page].Length ? pages[page][screen] ?? "welp" : "nothing";
            screenTexts[screen].fontSize = 7 < pages[page][screen].Length ? 35 : 40;//screen < pages[page].Length ? pages[page][screen].FontSize : 40;
        }
        if (arrowTexts != null && arrowTexts.Length >= 2)
        {
            arrowTexts[0].text = '<'.ToString(); //(pages[page].LeftArrow ?? '<').ToString();
            arrowTexts[1].text = '>'.ToString(); //(pages[page].RightArrow ?? '>').ToString();
        }
    }

    protected virtual void submitWord(KMSelectable submitButton)
    {
        if (moduleSolved)
            return;

        submitButton.AddInteractionPunch();
        if (screenTexts[2].text.Equals(answer))
        {
            Audio.PlaySoundAtTransform("SolveSFX", transform);
            module.HandlePass();
            moduleSolved = true;
            screenTexts[2].text = "";
        }
        else
        {
            Audio.PlaySoundAtTransform("StrikeSFX", transform);
            module.HandleStrike();
            page = 0;
            getScreens();
            submitScreen = false;
        }
    }

    protected virtual void letterPress(KMSelectable pressed)
    {
        if (moduleSolved)
            return;

        pressed.AddInteractionPunch(.2f);
        Audio.PlaySoundAtTransform("KeyboardPress", transform);
        if (submitScreen)
        {
            if (screenTexts[2].text.Length < 6)
                screenTexts[2].text += pressed.GetComponentInChildren<TextMesh>().text;
        }
        else
        {
            submitText.text = "UwU";
            screenTexts[0].text = "";
            screenTexts[1].text = "";
            screenTexts[2].text = pressed.GetComponentInChildren<TextMesh>().text;
            screenTexts[2].fontSize = 40;
            submitScreen = true;
        }
    }

    protected virtual void Log(string message)
    {
        Debug.LogFormat("[Fur Cipher #{0}] {1}", moduleId, message);
    }
    protected void Log(string format, params object[] args)
    {
        Log(string.Format(format, args));
    }

#pragma warning disable 414
    protected string TwitchHelpMessage = "!{0} right/left/r/l [move between screens] | !{0} submit answerword";
#pragma warning restore 414

    protected virtual IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.EqualsIgnoreCase("right") || command.EqualsIgnoreCase("r"))
        {
            yield return null;
            rightArrow.OnInteract();
            yield return new WaitForSeconds(0.1f);

        }
        if (command.EqualsIgnoreCase("left") || command.EqualsIgnoreCase("l"))
        {
            yield return null;
            leftArrow.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2 || !split[0].Equals("SUBMIT") || split[1].Length != 6)
            yield break;

        int[] buttons = split[1].Select(getPositionFromChar).ToArray();
        if (buttons.Any(x => x < 0))
            yield break;

        yield return null;
        yield return new WaitForSeconds(0.1f);
        foreach (char let in split[1])
        {
            keyboard[getPositionFromChar(let)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.1f);
        submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }

    protected IEnumerator TwitchHandleForcedSolve()
    {
        if (submitScreen && !answer.StartsWith(screenTexts[2].text))
        {
            rightArrow.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        int start = submitScreen ? screenTexts[2].text.Length : 0;
        for (int i = start; i < 6; i++)
        {
            keyboard[getPositionFromChar(answer[i])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }

    private int getPositionFromChar(char c)
    {
        return "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);
    }

    void Update()
    {
        if (moduleSelected)
        {
            for (var ltr = 0; ltr < 26; ltr++)
                if (Input.GetKeyDown(((char) ('a' + ltr)).ToString()))
                    keyboard[getPositionFromChar((char) ('A' + ltr))].OnInteract();
            if (Input.GetKeyDown(KeyCode.Return))
                submit.OnInteract();
        }
    }
    #endregion
}