import requests
from bs4 import BeautifulSoup
import smtplib
import os
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Scraper function
def scrape_hackathons():
    url = "https://mlh.io/seasons/2024/events"
    response = requests.get(url)
    soup = BeautifulSoup(response.text, 'html.parser')
    
    hackathons = []
    for event in soup.select(".event"):
        title = event.select_one("h3").text.strip()
        date = event.select_one(".event-date").text.strip()
        location = event.select_one(".event-location").text.strip()
        link = event.select_one("a")['href']
        
        if "South Africa" in location:
            hackathons.append(f"{title} - {date} - {location} - {link}")
    
    return hackathons

# Email sending function
def send_email(hackathons):
    sender_email = os.getenv("EMAIL_SENDER")
    receiver_email = os.getenv("EMAIL_RECEIVER")
    password = os.getenv("EMAIL_PASSWORD")
    
    subject = "Upcoming Hackathons in South Africa!"
    body = "\n".join(hackathons)
    
    msg = MIMEMultipart()
    msg['From'] = sender_email
    msg['To'] = receiver_email
    msg['Subject'] = subject
    
    msg.attach(MIMEText(body, 'plain'))
    
    try:
        with smtplib.SMTP('smtp.gmail.com', 587) as server:
            server.starttls()
            server.login(sender_email, password)
            server.sendmail(sender_email, receiver_email, msg.as_string())
        print("Email sent successfully!")
    except Exception as e:
        print(f"Error sending email: {e}")

if __name__ == "__main__":
    hackathons = scrape_hackathons()
    if hackathons:
        send_email(hackathons)
    else:
        print("No hackathons found in South Africa.")
