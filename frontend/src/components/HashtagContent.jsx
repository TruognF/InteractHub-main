export default function HashtagContent({ content, onHashtagClick }) {
  // Regex để tìm hashtag (bắt đầu bằng # theo sau là chữ cái hoặc số)
  const hashtagRegex = /#[a-zA-Z0-9_]+/g;
  
  const parts = content.split(hashtagRegex);
  const hashtags = content.match(hashtagRegex) || [];
  
  const elements = [];
  
  for (let i = 0; i < parts.length; i++) {
    // Thêm text bình thường
    if (parts[i]) {
      elements.push(
        <span key={`text-${i}`}>
          {parts[i]}
        </span>
      );
    }
    
    // Thêm hashtag nếu có
    if (i < hashtags.length) {
      elements.push(
        <a
          key={`hashtag-${i}`}
          className="post-hashtag"
          onClick={(e) => {
            e.preventDefault();
            onHashtagClick(hashtags[i]);
          }}
        >
          {hashtags[i]}
        </a>
      );
    }
  }
  
  return <>{elements}</>;
}
