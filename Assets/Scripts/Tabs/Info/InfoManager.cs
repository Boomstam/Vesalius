using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoManager : MonoBehaviour
{
    [System.Serializable]
    public struct ChapterData
    {
        public string title;
        [TextArea(4, 12)]
        public string content;
        public Sprite backgroundImage;
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI infoTitleText;
    [SerializeField] private TextMeshProUGUI infoContentText;
    [SerializeField] private Button previousInfoButton;
    [SerializeField] private Button nextInfoButton;
    [SerializeField] private Image infoBackground;

    [Header("Chapters")]
    [SerializeField] private ChapterData[] chapters = new ChapterData[]
    {
        new ChapterData { title = "De humani corporis fabrica", content = "Vesalius was a pioneering anatomist who transformed the study of the human body through direct observation and careful dissection. Challenging centuries of accepted wisdom, he documented the structure and function of every system with extraordinary detail. His work revealed the complexity and interconnectedness of bones, muscles, organs, vessels, and nerves, showing the body not as a collection of separate parts but as an integrated, living whole." },
        new ChapterData { title = "I. The Bones", content = "Vesalius began with the skeletal system, emphasizing careful observation over inherited assumptions. He corrected misconceptions about the number and structure of bones, showing how they form the framework that supports and shapes the human body." },
        new ChapterData { title = "II. The Muscles", content = "In his second book, Vesalius explored muscles, describing their arrangement, attachment points, and function. He highlighted the interplay between bones and muscles, explaining how coordinated movements are produced." },
        new ChapterData { title = "III. The Veins and Arteries", content = "Vesalius mapped the vascular system with unprecedented detail. He described the paths of veins and arteries, noting the differences in structure and function, which challenged long-standing beliefs inherited from Galen." },
        new ChapterData { title = "IV. The Nervous System", content = "Focusing on the brain, spinal cord, and nerves, Vesalius examined how signals travel to control sensation and movement. His careful dissections revealed the complexity of the nervous network and its central role in the body's functions." },
        new ChapterData { title = "V. The Organs of Digestion and Reproduction", content = "Vesalius analyzed the stomach, intestines, liver, and reproductive organs, emphasizing their structure and interrelation. He corrected errors in previous texts and linked form to function, showing the organs as integrated systems." },
        new ChapterData { title = "VI. The Heart and Lungs", content = "Here, Vesalius detailed the structure of the heart, lungs, and thoracic cavity. He explained the mechanical aspects of circulation and respiration, paving the way for later discoveries about blood flow and pulmonary function." },
        new ChapterData { title = "VII. The Brain and Senses", content = "In his final book, Vesalius returned to the head, examining the brain, eyes, ears, and other sense organs. He described how perception and cognition arise from complex anatomical structures, blending physiology with observation." }
    };

    private int _currentIndex = 0;

    private void Start()
    {
        previousInfoButton.onClick.AddListener(OnPrevious);
        nextInfoButton.onClick.AddListener(OnNext);
        DisplayChapter(_currentIndex);
    }

    private void OnDestroy()
    {
        previousInfoButton.onClick.RemoveListener(OnPrevious);
        nextInfoButton.onClick.RemoveListener(OnNext);
    }

    private void OnPrevious()
    {
        _currentIndex = (_currentIndex - 1 + chapters.Length) % chapters.Length;
        DisplayChapter(_currentIndex);
    }

    private void OnNext()
    {
        _currentIndex = (_currentIndex + 1) % chapters.Length;
        DisplayChapter(_currentIndex);
    }

    private void DisplayChapter(int index)
    {
        infoTitleText.text = chapters[index].title;
        infoContentText.text = chapters[index].content;
        infoBackground.sprite = chapters[index].backgroundImage;
    }
}