-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Anamakine: 127.0.0.1
-- Üretim Zamanı: 28 Eki 2024, 23:34:58
-- Sunucu sürümü: 10.4.32-MariaDB
-- PHP Sürümü: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Veritabanı: `bets`
--

-- --------------------------------------------------------

--
-- Tablo için tablo yapısı `teams`
--

CREATE TABLE `teams` (
  `teamID` int(11) NOT NULL,
  `teamName` varchar(255) NOT NULL,
  `leagueName` varchar(255) NOT NULL,
  `howManyDraws` int(11) NOT NULL,
  `linkOfTeam` varchar(255) NOT NULL,
  `lastDrawnMatchDate` varchar(255) NOT NULL,
  `isBetsOn` int(11) NOT NULL,
  `howMuchWasBet` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Tablo döküm verisi `teams`
--

INSERT INTO `teams` (`teamID`, `teamName`, `leagueName`, `howManyDraws`, `linkOfTeam`, `lastDrawnMatchDate`, `isBetsOn`, `howMuchWasBet`) VALUES
(0, 'Torino', 'Serie A', 5, 'https://www.mackolik.com/takim/torino/ma%C3%A7lar/7gnly6999wao1xarwct4p8fe9', '0', 0, 0),
(1, 'Genoa', 'Serie A', 1, 'https://www.mackolik.com/takim/genoa/ma%C3%A7lar/4kumqzwifv478caxed8zywlh3', '0', 0, 0),
(2, 'Lecce', 'Serie A', 4, 'https://www.mackolik.com/takim/lecce/ma%C3%A7lar/bi1fxjrncd0ram0oi7ja1jyuo', '0', 0, 0),
(3, 'Empoli', 'Serie A', 0, 'https://www.mackolik.com/takim/empoli/ma%C3%A7lar/8le3orkfz6iix3jns6g9ojqjg', '0', 0, 0),
(4, 'Cagliari', 'Serie A', 2, 'https://www.mackolik.com/takim/cagliari/ma%C3%A7lar/5rwlg5cfv1hu7yf0ek1zxpzy3', '0', 0, 0),
(5, 'Hellas Verona', 'Serie A', 9, 'https://www.mackolik.com/takim/hellas-verona/ma%C3%A7lar/ap6blbxhq9elm62vw6tutzlwg', '04.10.2024.2024', 1, 40),
(6, 'Sassuolo', 'Serie A', 3, 'https://www.mackolik.com/takim/sassuolo/ma%C3%A7lar/6tuibxq39fdryu8ou06wcm0q3', '0', 0, 0);

--
-- Dökümü yapılmış tablolar için indeksler
--

--
-- Tablo için indeksler `teams`
--
ALTER TABLE `teams`
  ADD PRIMARY KEY (`teamID`);

--
-- Dökümü yapılmış tablolar için AUTO_INCREMENT değeri
--

--
-- Tablo için AUTO_INCREMENT değeri `teams`
--
ALTER TABLE `teams`
  MODIFY `teamID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=36;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
