FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY app ./app
COPY data/resolution_knowledge_base.json ./data/resolution_knowledge_base.json
COPY models ./models
COPY run.py .
EXPOSE 5055
CMD ["python","run.py"]
