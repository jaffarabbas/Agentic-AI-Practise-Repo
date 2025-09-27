import os
from agents.run import set_default_agent_runner
from dotenv import load_dotenv
from agents import Agent, OpenAIChatCompletionsModel, RunConfig, Runner, set_default_openai_api, set_default_openai_client, set_tracing_disabled
from openai import AsyncOpenAI
from config import AIProviderConfig

load_dotenv()
set_tracing_disabled(True)

client = AsyncOpenAI(
            api_key=os.getenv("OPENAI_API_KEY"),
            base_url=os.getenv("OPENAI_API_BASE_URL")
        )

model = OpenAIChatCompletionsModel(
            model="gpt-4o-mini",  # Default OpenAI model
            openai_client=client,
        )

config = RunConfig(
    model=model,
    tracing_disabled=True
)

agent: Agent = Agent(
    name="my-agent",
)

runner = Runner.run_sync(starting_agent=agent, input="what is mcp in ai world?", config=config)