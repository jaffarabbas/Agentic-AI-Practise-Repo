import os
from agents.run import set_default_agent_runner
from dotenv import load_dotenv
from agents import Agent, RunConfig, Runner, function_tool, set_default_openai_api, set_default_openai_client, set_tracing_disabled
from openai import AsyncOpenAI
from tavily import TavilyClient

load_dotenv()
set_tracing_disabled(True)
set_default_openai_api("chat_completions")

external_client = AsyncOpenAI(
    api_key=os.getenv("OPENAI_API_KEY"),
    base_url=os.getenv("OPENAI_API_BASE_URL")
)

tavilo_agent = TavilyClient(api_key=os.getenv("TAVILI_API_KEY"))

set_default_openai_client(external_client)

#tools
@function_tool
def get_weather(city: str) -> str:
    return f"The weather in {city} is sunny"

@function_tool
def get_news(topic: str) -> str:
    #integrate travilly
    response = tavilo_agent.search(topic)   
    return response

@function_tool
def get_stock_price(stock: str) -> str:
    return f"The stock price of {stock} is 100"

agent: Agent = Agent(
    model='gpt-4o-mini',
    name="my-agent",
    tools=[get_weather, get_news, get_stock_price],
)

runner = Runner.run_sync(starting_agent=agent, input="what is the latest update of nvidia")

print(runner.final_output)