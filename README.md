### **Martingale Software for Tracking Draw Probabilities Across Various Leagues**

This software uses the Martingale strategy to monitor teams across multiple leagues around the world, checking their recent match results to identify teams that haven't had a draw in their last five games. It avoids teams at the top or bottom of the league, as average-level teams have a higher likelihood of drawing compared to these extremes.

![](https://github.com/barismuutlu/Gambling-with-Martin-Gale/blob/master/images/martingale.png?raw=true)

The application performs the following tasks:
- Regularly checks match results of specified teams from various leagues (based on data from *mackolik.com*) and updates their latest scores in the database.
- Sends notifications to the user’s Discord server with:
   1. The last draw occurrence for each team.
   2. Recommended bet amount per match for each team.
   3. Any errors encountered in the application.

The project utilizes Selenium, SQL, and the Discord API. An `.sql` file is included in the project for database setup, which can be run locally using XAMPP or a similar tool.

**Planned Improvements for the Next Version:**
- Replace Selenium with HTTP Requests for improved speed and efficiency.
- Develop a more dynamic message algorithm for Discord notifications.

**Note:** This project is for educational purposes only. Martingale is a risky gambling strategy, as even a few rounds of betting can lead to exponentially large sums due to its O(2^n) nature!

![](https://github.com/barismuutlu/Gambling-with-Martin-Gale/blob/master/images/software1.png?raw=true)
![](https://github.com/barismuutlu/Gambling-with-Martin-Gale/blob/master/images/software2.png?raw=true)
![](https://github.com/barismuutlu/Gambling-with-Martin-Gale/blob/master/images/discord1.png)
![](https://github.com/barismuutlu/Gambling-with-Martin-Gale/blob/master/images/discord1.png)
![](https://github.com/barismuutlu/Gambling-with-Martin-Gale/blob/master/images/mysql.png?raw=true)
