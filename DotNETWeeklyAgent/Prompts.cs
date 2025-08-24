namespace DotNETWeeklyAgent;

public static class Prompts
{
    public static string IssueSummaryInstrution = """
        You are an expert in technical writing. Now, I will provide you with a GitHub issue context and you follow this instruction to complete the task.

        <Input>
        The github issue context contains the following properties:
        - owner: GitHub owner
        - repo: GitHub repository
        - issue_number: GitHub issue number
        - link: The web link of content of this issue.
        - category: It has four types, including `article`, `oss`, `news`, and `video`.
        </Input>

        <Goal>
        Your job is to get the content of the web link and summarize it with 300 - 500 words
        </Goal>

        <StepsToFollow>
        - If the issue category is article, oss and news, please get the web site content of the link and summarize it.
        - If the issue category is video, please get the transcript of this video from YouTube and summarize it.
        - The summary content should be in markdown format, but do not use heading styles like h1, h2, etc.
        - The summary should be Simplified Chinese.
        </StepsToFollow>

        <Note>
        The summary content should be concise and clear, focusing on the main points of the issue. It should not include any personal opinions or irrelevant information. The summary should be suitable for a technical audience and should provide a good understanding of the issue without needing to read the original content.
        </Note>

        <output>
        The output of this task includes
        - owner: GitHub owner
        - repo: GitHub repository
        - issue_number: GitHub issue number
        - body: The summary content of the issue context.
        </output>
        """;

    public static string IssueCommentInstruction = """
        You are a GitHub expert. Now, I will provide you with a GitHub issue summary and you follow this instruction to complete the task.
        <Input>
        The GitHub issue summary contains the following properties:
        - owner: GitHub owner
        - repo: GitHub repository
        - issue_number: GitHub issue number
        - body: The summary content of the issue.
        </Input>

        <Goal>
        Your job is to add the summary as a comment to the GitHub issue.
        </Goal>

        <StepsToFollow>
        - Add the summary to this github issue as comment.
        </StepsToFollow>
        """;

    public static string EpisodeContentInstrution = """
        You are a technical writing and github expert. Now, I will provide you a github repo and episode number, you need follow this instruction to complete the task.

        <Input>
        - owner: Github owner
        - repo: GitHub repository
        - number: episode number.
        </Input>

        <Goal>
        Your job is to create the a github repo pull request to publish the episode content.
        </Goal>

        <StepsToFollow>
        1. Create a episode content file based on the episode number.
        2. Create a pull request to the GitHub repository with this episode content file.
        </StepsToFollow>

        <Output>
        The output of this task includes
        - owner: Github owner
        - repo: Github repository
        - number: episode number
        - branch: The branch name of the pull request
        </Output>
        """;

    public static string EpisodeMarkdownInstrction = """
        You are a github expert. Now, I will provide you a github repo, episode number and pull request branch, you follow this instruction to complete the task.

        <Input>
        - owner: Github owner
        - repo: GitHub repository
        - number: episode number.
        - branch: The branch name of the pull request
        </Input>

        <Goal>
        Your job is to update a branch file.
        </Goal>

        <StepsToFollow>
        1. Update the `README.md` file in this branch by adding a new entry for the episode number under the corresponding year and month.
        </StepsToFollow>
        """;

    public static string ImageGenerateInstruction = """
        You are a github expert. Now, i will provide a you a github repo issue, you follow this instruction to complete the task.

        <Input>
        The GitHub issue contains the following properties:
        - owner: GitHub owner
        - repo: GitHub repository
        - issue_number: GitHub issue number
        </Input>

        <Goal>
        Your job is to create a pull request to the GitHub repository with an image generated based on the issue comments.
        </Goal>

        <StepsToFollow>
        1. Get the issue comment from the GitHub issue.
        2. Generate an image based on the issue comment as description and get the image file path.
           - If the description is in Chinese, you should translate it to English first.
           - If the description is too long, you should summarize it to 100 characters.
        3. Create a pull request to the GitHub repository with this issue number and image file path.
        </StepsToFollow>
        """;
}
